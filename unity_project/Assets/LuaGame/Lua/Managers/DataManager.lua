---
--- Created by User.
--- DateTime: 2019-08-12 17:28
---

require "Common.functions"
--local async = require "3rd.luvit-async.init"
local step = require("3rd.lua-step.src.step")

---
--- 为了性能优化，将解析后的json对象缓存在lua端。否则每次从C#获取json都要取string，然后解析成json对象，影响效率。
---
DataManager = {};
DataManager.DataTableNames =
{

};

--key: relatedPath, value:json
local allDataLoaded = false;
DataManager.dataDict = {};

function DataManager.AllDataLoaded()
    return allDataLoaded;
end

function DataManager.Init()
    ---TODO: GL - 暂时放在这里，将来在合适的时机加载。比如热更新之后
    --DataManager.LoadAllData();
    allDataLoaded = false;
end

---
--- 通过名字获取一个表数据（table）
---
function DataManager.GetData(name)
    if(DataManager.dataDict[name] == nil) then
        local err = "Not found this data!-"..name;
        Debug.LogError(err);
    end
    return DataManager.dataDict[name];
end

---
--- 通过名字和id获取一个数据实体（表，行）
---
function DataManager.GetDataEntity(name, id)
    local data = DataManager.GetData(name);
    local entity = data[id];
    return entity;
end
---
--- 加载Table数据。
--- 在CS的DataSystem中加载，lua这边做一个json对象的缓存。
--- DataSystem中也有一份CS的JSON对象缓存，供CS代码使用。
---
function DataManager.LoadData(name, callback)
    Debug.Log('[TODO]-->load-file:'..name);
    local data = {};
    DataSystem.LoadDataX(name, function(text)
        --Debug.Log('+++load-file:'..name);
        --Debug.Log('load-text:'..text);
        data = DataManager.ParseToJsonObject(text);
        DataManager.dataDict[name] = data;
        if(callback ~= nil) then
            callback(data);
        end
    end);
end

---由于LoadData是异步方法，所以需要做异步的等待处理，等所有数据都加载完之后再继续流程。
function DataManager.LoadAllData(callback)
    local tryActions = {};
    --GL - 以下注释代码可以作为step(异步流程控制)的瀑布式流程的范例。
    --[[local tryActions1 = {
        [1] = function(step)
            Debug.Log("**第一步");
            DataManager.LoadData("CardData", function()
                step.success();
            end);
        end,
        [2] = function(step)
            Debug.Log("**第二步");
            DataManager.LoadData("CardSkillData", function()
                step.success();
            end);
        end,
        [3] = function(step)
            Debug.Log("**第三步");
            DataManager.LoadData("AuraEffectData", function()
                step.success();
            end);
        end,
    };
    --]]
    local tableCount = table.getn(DataManager.DataTableNames);
    for i = 1, tableCount do
        tryActions[i] = function(step)
            local name = DataManager.DataTableNames[i];
            GameDebugger.sPushLog("-->第"..i.."步:"..name);
            DataManager.LoadData(name, function()
                step.success();
            end);
        end
    end
    local loadActions = step.new({
        --tryActions,
        try = tryActions,
        catch = function(step, err)
            GameDebugger.sPushLog("-->Loading tables error:"..err);
            if(callback ~= nil) then
                callback(err);
            end
        end,
        finally = function(step)
            GameDebugger.sPushLog("-->Loading tables finished!");
            allDataLoaded = true;
            if(callback ~= nil) then
                callback(nil);
            end
        end,
        }
    );
    GameDebugger.sPushLog("-->Start loading all tables! table count:"..tableCount);
    loadActions();
end

function DataManager.LoadAllDataFromCS()
    local textTables = DataSystem.GetTextTables();
    local iter = textTables:GetEnumerator();
    while iter:MoveNext() do
        local key = iter.Current.Key
        if key ~= 'TextData' then
            local value = iter.Current.Value
            --print('>>>>key:'..key);
            --print('>>>>value:'..value);
            DataManager.dataDict[key] = DataManager.ParseToJsonObject(value);
        end
    end
end

function DataManager.ParseToJsonObject(text)
    local data = {};
    local jsonObject = cjson.decode(text);
    if(jsonObject == nil) then
        local err = "Load json data failed!-"..name;
        Debug.LogError(err);
    end
    for i, v in ipairs(jsonObject) do
        --print('>>i:'..i);
        local id = tonumber(v.ID);
        --print('>>ID:'..id);
        data[id] = v;
    end
    return data;
end

