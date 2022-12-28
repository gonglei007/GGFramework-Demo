--[[
--Created Date: Thursday, November 30th 2017, 5:40:38 pm
--Author: Yu GuoPing (ccwigg@qq.com)
--Copyright © 2017 - 2017, GoTech.Co.Ltd
--]]

LuaDelegate = {}

function LuaDelegate:New()
    local event = {};
    event.tag = "LuaDelegate";
    setmetatable(event, self)
    --重写__index方法
    self.__index = self;
    --重写__call方法
    self.__call = self.Call;
    --重写加号运算
    self.__add = self.opt_Add;
    --重写减号运算
    self.__sub = self.opt_Sub;
    return event;
end

--添加事件
function LuaDelegate:Add(func)
    table.insert(self, func);
 end

--移除事件
function LuaDelegate:Remove(func)
    for i = 1, #self do
        if (self[i] == func) then
            self[i] = nil;
            break;
        end
    end
end

--如果委托中不存在传入的事件，才会添加
function LuaDelegate:AddIfNotExist(func)
    local pass = true;
    for i = 1, #self do
        if (self[i] == func) then
            pass = false;
            break;
        end
    end
    if (pass) then
        table.insert(self, func);
    end
end

--清空事件
function LuaDelegate:Clear()
    for i = 1, #self do
        self[i] = nil;
    end
end


function LuaDelegate.Call(o, ...)
    for i = 1, #o do
        if (o[i]) then
            o[i](...);
        end
    end
end

function LuaDelegate.opt_Add(o, a)
    local checkType = LuaDelegate.CheckType(a);
    if (checkType > 0) then
        o:Add(a);
        return o;
    else
        error("LuaDelegate: opt_Add arg type error");
        return o;
    end

end

function LuaDelegate.opt_Sub(o, a)
    local checkType = LuaDelegate.CheckType(a);
    if (checkType > 0) then
        o:Remove(a);
        return o;
    else
        error("LuaDelegate: opt_Sub arg type error");
        return o;
    end
end


 --检查传入参数的类型，1:function, 2:LuaDelegate， 0：其他
function LuaDelegate.CheckType(a)
    local t = type(a);
    --Debug.LogWarning("type:" .. t);
    if (t == "function") then
        return 1;
    elseif (a and a.tag and a.tag == "LuaDelegate") then
        return 2;
    end
    return 0;
end

--example
function LuaDelegate.Test()
    local tstDelegate = LuaDelegate:New();

    local foo1 = function()
        print("aaaa1");
    end

    local foo2 = function(sss)
        if (sss ~= nil) then
            print("bbbb3" .. sss);
        else
            print("cccc5");
        end
    end

    local tst2 = LuaDelegate:New();

    tstDelegate();
    tstDelegate = tstDelegate - foo1;
    tstDelegate = tstDelegate + foo1;
    tstDelegate = tstDelegate - foo1;
    tstDelegate();

    --不推荐tstDelegate = foo1的写法，
    --这样子做以后tstDelegate将不再是LuaDelegate类型，用以下两行代替：
    tstDelegate:Clear();
    tstDelegate = tstDelegate + foo1;

    tstDelegate:AddIfNotExist(foo1);
    tstDelegate();

    tst2 = tst2 + foo2;
    tst2("hhh");
end