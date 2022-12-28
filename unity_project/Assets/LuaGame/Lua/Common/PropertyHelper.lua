--[[
--Created Date: Thursday, March 29th 2018, 5:15:52 pm
--Author: Yu GuoPing (ccwigg@qq.com)
--Copyright © 2017 - 2017, GoTech.Co.Ltd
--]]

PropertyHelper = {};

local RegularMetaHandle = {};

-- 给某个table设置属性访问器（实验性质）
-- 注意：这个方法会改变元表查找行为（setmetable, __index, __newindex相关)，使用前请考虑后果
-- 目前不考虑继承的处理
function PropertyHelper.Setup(o)
    local getter = {};
    local setter = {};
    local handle = RegularMetaHandle.Create(getter, setter);

    setmetatable(o, handle)

    return getter, setter;
end

-- 常规元表处理模型
function RegularMetaHandle.Create(getter, setter)
    local handle = {};
    handle.get = getter;
    handle.set = setter;

    handle.__index = function(obj, key)
        local result = rawget(obj, key);

        if (result == nil) then
            result = getter[key];

            if (result ~= nil) then
                return result(obj);
            end
        end

        return result;
    end;


    handle.__newindex = function(obj, key, value)
        local result = rawget(obj, key);

        --实质上当raw data里存在对应key的时候，__newindex根本不会被调用
        if (result ~= nil) then
            rawset(obj, key, value);
            return;
        else
            result = setter[key];
            if (result ~= nil) then
                result(obj, value);
                return;
            end
        end
        -- 如果都查不到key的话，给obj本身添加索引和值，而不加在元表里
        rawset(obj, key, value);
    end;

    return handle;
end
