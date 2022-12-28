--[[
--Created Date: Wednesday, January 17th 2018, 12:08:05 pm
--Author: Yu GuoPing (ccwigg@qq.com)
--Copyright © 2017 - 2017, GoTech.Co.Ltd
--]]

require "Common/LuaDelegate"

--计时类
SimpleTimer = {};

function SimpleTimer:New()
    local o = {};

    setmetatable(o, self);
    self.__index = self;

    --暂定就检测分和秒，其他的貌似用的不多
    o.Event_OnSecondChanged = LuaDelegate:New();
    o.Event_OnMinuteChanged = LuaDelegate:New();

    o.coFunc = nil;

    --流逝的时间，秒，浮点数
    o.elaspedTime = 0;

    --整数
    o.elaspedSecond = 0;

    --整数
    o.elaspedMinute = 0;

    --防止一些情况下协程无法关闭
    o.run = false;
    o.paused = false;
    return o;

end

function SimpleTimer:Reset()
    self:Stop();
    self.elaspedTime = 0;
    self.elaspedSecond = 0;
    self.elaspedMinute = 0;
    self.paused = false;
end

function SimpleTimer:Start(useUnscaledTime)
    if (self.run == true) then
        return;
    end
    if (useUnscaledTime == nil) then
        useUnscaledTime = true;
    end
    self.run = true;
    self.paused = false;
    self.coFunc = coroutine.start(function()

        while (self.run == true) do
            coroutine.step();
            if (self.paused == false) then
                if (useUnscaledTime == true) then
                    self:UpdateTime(Time.unscaledDeltaTime);
                else
                    self:UpdateTime(Time.deltaTime);
                end
            end
        end

    end);
end

function SimpleTimer:UpdateTime(deltaTime)
    self.elaspedTime = self.elaspedTime + deltaTime;
    local second = GetIntPart(self.elaspedTime);
    local minute = GetIntPart(self.elaspedTime / 60);
    if (self.elaspedSecond ~= second) then
        self.Event_OnSecondChanged();
    end
    if (self.elaspedMinute ~= minute) then
        self.Event_OnMinuteChanged();
    end

end

--获取hour、minute、second信息,已经经过取模、取整处理
function SimpleTimer:GetTimeInfo()
    local hour;
    local minute;
    local second;

    local tempSecond = GetIntPart(self.elaspedTime);

    second = tempSecond % 60;
    local tempMinute = GetIntPart(tempSecond / 60);
    minute = tempMinute % 60;
    hour = GetIntPart(tempMinute / 60);

    return hour, minute, second;
end

function SimpleTimer:GetElaspedTime()
    return self.elaspedTime;
end

function SimpleTimer:Pause()
    self.paused = true;
end

function SimpleTimer:Resume()
    self.paused = false;
end

function SimpleTimer:Stop()
    self.run = false;
    if (self.coFunc) then
        coroutine.stop(self.coFunc);
        self.coFunc = nil;
    end
end
