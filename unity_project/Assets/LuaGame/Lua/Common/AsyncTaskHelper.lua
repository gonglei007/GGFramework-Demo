AsyncTaskHelper = {};

function AsyncTaskHelper:New(obj)
    obj = obj or {};

    setmetatable(obj, self);
    self.__index = self;

    --最多有几个任务同时工作
    obj.maxWorkingTaskCount = 4;
    --同一帧内最多执行几次循环
    obj.taskStep = 1;
    --是否等待当前优先级的任务全部完成后，下一优先级的任务才允许进入工作队列
    obj.allowPriorityMix = false;

    obj.taskList = {};
    obj.taskCount = 0;
    obj.totalProcess = 0;
    obj.currentProcess = 0;
    obj.finishedTaskCount = 0;

    obj.workingTaskList = {};
    obj.workingTaskCount = 0;

    obj.priorityToTargetCountDict = {};
    obj.priorityToFinishCountDict = {};

    obj.finishCallback = nil;
    obj.processCallback = nil;

    obj.isFinished = false;

    obj.coFunc = nil;
    obj.coHandler = nil;

    return obj;
end

function AsyncTaskHelper:SetMaxWorkingTaskCount(count)
    if (count > 0) then
        self.maxWorkingTaskCount = count;
    end
end
function AsyncTaskHelper:SetTaskStep(step)
    if (step > 0) then
        self.taskStep = step;
    end
end
function AsyncTaskHelper:AllowPriorityMix(flag)
    self.allowPriorityMix = flag;
end

--func<callback<processValue [0 - 1]>>
--- function sum description.
-- Some description, can be over several lines.
-- @param func func<callback<processValue [0 - 1]>>
-- @param priority 任务优先级
-- @param taskWeight 任务权重
function AsyncTaskHelper:RegistTask(func, priority, taskWeight)
    if (not priority) then
        priority = 1;
    elseif priority == 0 then
        error("AsyncTaskHelper:RegistTask : 参数priority 不要传0");
    end

    if (not taskWeight) then
        taskWeight = 1;
    end

    local handler = AsyncTaskHandler:New(nil, self.taskCount + 1, func, priority, taskWeight);
    table.insert(self.taskList, handler);

    self.totalProcess = self.totalProcess + taskWeight;
    self.taskCount = self.taskCount + 1;
end
function AsyncTaskHelper:StartTasks()
    if (not self.coFunc) then
        self.coFunc = function() self:HandleTaskCoFun(); end;
    end
    self.coHandler = coroutine.start(self.coFunc);
end
function AsyncTaskHelper:HandleTaskCoFun()

    if (self.taskCount <= 0) then
        if (self.finishCallback) then
            self.finishCallback();
        end
        return;
    end

    table.sort(self.taskList,function(a,b)
            return a.priority < b.priority;
        end);

    for k = 1, #self.taskList do
        if (not self.priorityToTargetCountDict[k]) then
            self.priorityToTargetCountDict[k] = 0;
            self.priorityToFinishCountDict[k] = 0;
        end
        self.priorityToTargetCountDict[k] =  self.priorityToTargetCountDict[k] + 1;
    end


    local i = 1;
    local currentPriority = self.taskList[1].priority;
    local step = 0;

    while (i <= #self.taskList) do
        if (self.workingTaskCount < self.maxWorkingTaskCount) then

            local pass = false;

            local handler = self.taskList[i];

            if (self.allowPriorityMix) then
                pass = true;
            else
                if (currentPriority == handler.priority) then
                    pass = true;
                else
                    --当前优先级的任务全部完成了的情况
                    if (self.priorityToFinishCountDict[currentPriority] >= self.priorityToTargetCountDict[currentPriority]) then
                        pass = true;
                    else
                        pass = false;
                    end
                end
            end

            if (pass) then
                table.insert(self.workingTaskList, handler);
                self.workingTaskCount = self.workingTaskCount + 1;
                handler.func(function (processValue)

                    if (handler.isFinished == false) then
                        --更新任务进度
                        local cProcess = processValue;

                        if (cProcess > 1) then
                            cProcess = 1;
                        end
                        if (cProcess < 0) then
                            cProcess = 0;
                        end

                        --如果进度满足条件，完成任务
                        if (processValue >= 1) then
                            handler.isFinished = true;
                            self:RemoveFromWorkingList(handler);
                            self.finishedTaskCount = self.finishedTaskCount + 1;
                            self.priorityToFinishCountDict[handler.priority] = self.priorityToFinishCountDict[handler.priority] + 1;
                            currentPriority = handler.priority;

                        end
                        self:RenewTaskProcess(handler.lastProcess * handler.taskWeight, cProcess * handler.taskWeight);
                        handler.lastProcess = cProcess;

                        if (processValue >= 1) then
                            if (self.finishedTaskCount == self.taskCount) then
                                if (self.finishCallback) then
                                    self.isFinished = true;
                                    self.finishCallback();
                                end
                            end
                        end
                    end

                end);

                i = i + 1
            end

        end

        step = step + 1;
        if (step >= self.taskStep) then
            --yield return 0
            coroutine.step();
            step = 0;
        end

    end

end

function AsyncTaskHelper:RemoveFromWorkingList(taskHandler)
    if (taskHandler) then
        local pos = 0;
        for i = 1, #self.workingTaskList do
            if (self.workingTaskList[i] == taskHandler) then
                pos = i;
                break;
            end
        end

        if (pos > 0) then
            table.remove(self.workingTaskList, pos);
            self.workingTaskCount = self.workingTaskCount - 1;
        end
    end
end

function AsyncTaskHelper:RenewTaskProcess(oldProcess, newProcess)
    self.currentProcess = self.currentProcess - oldProcess + newProcess;

    if (self.processCallback) then
        self.processCallback(self.currentProcess, self.finishedTaskCount);
    end
end

--这个方法看上去毫无卵用
function AsyncTaskHelper:StopTasks()
    if (self.coHandler and (not self.isFinished)) then
        coroutine.stop(self.coHandler);
    end
end

--clearAll：默认为false， 是否清除所有设置的任务以及回调
function AsyncTaskHelper:Reset(clearAll)

    self:StopTasks();
    if (clearAll == true) then
        if (self.taskList) then
            for i = 1, #self.taskList do
                self.taskList[i] = nil;
            end
        end
        self.taskList = {};
        self.taskCount = 0;
        self.totalProcess = 0;

        self.finishCallback = nil;
        self.processCallback = nil;

        for k in pairs(self.priorityToTargetCountDict) do
            self.priorityToTargetCountDict[k] = nil;
        end

        self.priorityToTargetCountDict = {};
    end

    if (self.workingTaskList) then
        for i = 1, #self.workingTaskList do
            self.workingTaskList[i] = nil;
        end
    end


    self.currentProcess = 0;
    self.finishedTaskCount = 0;

    self.workingTaskList = {};
    self.workingTaskCount = 0;


    for k in pairs(self.priorityToFinishCountDict) do
        self.priorityToFinishCountDict[k] = nil;
    end
    
    self.priorityToFinishCountDict = {};

    self.isFinished = false;
end

function AsyncTaskHelper:SetFinishCallback(callback)
    self.finishCallback = callback;
end
-- Callback<currentProcess, finishedTaskCount>
function AsyncTaskHelper:SetProcessCallback(callback)
    self.processCallback = callback;
end

function AsyncTaskHelper:GetCurrentProcess()
    return self.currentProcess;
end
function AsyncTaskHelper:GetTotalProcess()
    return self.totalProcess;
end

function AsyncTaskHelper:GetFinishedTaskCount()
    return self.finishedTaskCount;
end

function AsyncTaskHelper:GetTotalTaskCount()
    return self.taskCount
end

AsyncTaskHandler = {};

function AsyncTaskHandler:New(obj, taskId, func, priority, taskWeight)
    local obj = {};
    
    setmetatable(obj, self);
    self.__index = self;

    --handler 在taskList中的索引
    obj.taskId = taskId;
    obj.func = func;
    obj.priority = priority;
    obj.taskWeight = taskWeight;

    obj.isFinished = false;
    obj.lastProcess = 0;

    return obj;
end

--[[
function AsyncTaskHelper.Test()
    local helper = AsyncTaskHelper:New();

    helper:RegistTask(function(callback)
        Util.LogWarning("i am task " .. 1);
        callback(1);
    end, 1, 1);

    helper:RegistTask(function(callback)
        coroutine.wait(2);
        callback(0.5);
        Util.LogWarning("i am task " .. 2);
        coroutine.wait(2);
        callback(1);
    end, 1, 1);

    helper:RegistTask(function(callback)
        Util.LogWarning("i am task " .. 3);
        callback(1);
    end, 3, 1);

    helper:RegistTask(function(callback)
        Util.LogWarning("i am task " .. 4);
        callback(1);
    end, 1, 1);

    helper:RegistTask(function(callback)
        Util.LogWarning("i am task " .. 5);
        callback(1);
    end, 1, 1);

    helper:SetProcessCallback(function(currentProcess, finishedTaskCount)
        Util.LogWarning("task process :" .. currentProcess .. "/" .. helper:GetTotalTaskCount());
    end);

    helper:SetFinishCallback(function()
        Util.LogWarning("task finish");
    end);

    helper:StartTasks();
end
--]]