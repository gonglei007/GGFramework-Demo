--日志类函数已经更新，详情参见c# Util以及对应Wrapp脚本
--带lua traceback版本请参见下方Debug类
--输出日志--
function log(str)
    Util.Log(str)
end

--错误日志--
function logError(str)
    Util.LogError(str)
end

--警告日志--
function logWarn(str)
    Util.LogWarning(str)
end

--查找对象--
function find(str)
    return GameObject.Find(str)
end

function destroy(obj)
    GameObject.Destroy(obj)
end

function newObject(prefab)
    return GameObject.Instantiate(prefab)
end

--创建面板--                                                                                                                                                                          nel(name)
function createPanel(name)
    nel(name)
    panelMgr:CreatePanel(name)
end

function child(str)
    return transform:FindChild(str)
end

function subGet(childNode, typeName)
    return child(childNode):GetComponent(typeName)
end

function findPanel(str)
    local obj = find(str)
    if obj == nil then
        error(str .. " is null")
        return nil
    end
    return obj:GetComponent("BaseLua")
end

function ResetHeadFrame(headIconObject, frameId, ishowEffect)
    FrameModel.ResetHeadFrame(headIconObject, frameId, ishowEffect)
end
--所有支付相关的接口 去找C# 脚本RechargeTool.PayStart
function PayStart(payId, giftId)
    if giftId == nil then
        giftId = 0
    end
    GONetworkManager_HF.isPaying = true
    RechargeTool.PayStart(tonumber(payId), tonumber(giftId))
end

function string.SubUTF8String(s, n) --   获取 中文时 用
    local dropping = string.byte(s, n + 1)
    if not dropping then
        return s
    end
    if dropping >= 128 and dropping < 192 then
        return string.SubUTF8String(s, n - 1)
    end
    return string.sub(s, 1, n)
end

function string.utf8len(input) --Lua 里面计算汉字的长度
    local len = string.len(input)
    local left = len
    local cnt = 0
    local arr = {0, 0xc0, 0xe0, 0xf0, 0xf8, 0xfc}
    while left ~= 0 do
        local tmp = string.byte(input, -left)
        local i = #arr
        while arr[i] do
            if tmp >= arr[i] then
                left = left - i
                break
            end
            i = i - 1
        end
        cnt = cnt + 1
    end
    return cnt
end

function ConversTime(time)

    if time <= 0 then
        return "00:00";
    end

    local min = math.floor(time / 60);
    local sec = math.floor(time % 60);

    local strMin, strSec, str;
    if min < 10 then
        strMin = string.format("0%d", min);
    else
        strMin = tostring(min);
    end
    if sec < 10 then
        strSec = string.format("0%d", sec);
    else
        strSec = tostring(sec);
    end
    --if hour > 0 then
    --    str = string.format("[FF3030]%s:%s", strHour, strMin);
    --else
    --    str = string.format("[61FD8AFF]%s:%s", strMin, strSec);
    --end
    str = string.format("%s:%s", strMin, strSec);

    return str;
end

function CheckTextLen(uiInputLabel, morethanNum)
    uiInputLabel = CommonTools.CleanStr(uiInputLabel)
    local num = CSString.Length(uiInputLabel)
    if num > morethanNum or uiInputLabel == "" or uiInputLabel == nil then
        return true
    else
        return false
    end
end
--获取table的长度
function table_len(t)
    if (t == nil) then
        return 0
    end
    local len = 0
    for k, v in pairs(t) do
        len = len + 1
    end
    return len
end

--判断table中是否包含某个key
function is_include_key(tab, key)
    for k, v in pairs(tab) do
        if k == key then
            return true
        end
    end
    return false
end

--判断table中是否包含某个value
function is_include_value(tab, value)
    for k, v in pairs(tab) do
        if v == value then
            return true
        end
    end
    return false
end

--注册事件回调，注意只有c# Delegate类型才可使用
function RegEvent(eventFun, callback)
    if (eventFun) then
        eventFun = eventFun + callback
    else
        eventFun = callback
    end
end

--注销事件回调，注意只有c# Delegate类型才可使用
function UnRegEvent(eventFun, callback)
    if (eventFun) then
        eventFun = eventFun - callback
    end
end

--**************************以下为参考方法，不能直接使用的**************************
--遍历字典参考方法
function IterDict(map)
    local iter = map:GetEnumerator()
    while iter:MoveNext() do
        local k = iter.Current.Key
        local v = iter.Current.Value
        return k, v
    end
end

function ClearTable(table)
    if (table and next(table)) then
        for k, v in pairs(table) do
            table[k] = nil
        end
    end
end

--取一个数的整数部分
function GetIntPart(x)
    if x <= 0 then
        return math.ceil(x)
    else
        return math.floor(x)
    end
end

----------------------Debug Extend Method Start
Debug = Debug or {}
--带lua trace的log
function Debug.Log(msg, obj)
    Util.LogEx(msg .. "\n" .. debug.traceback(), obj)
end

function Debug.LogWarning(msg, obj)
    Util.LogWarningEx(msg .. "\n" .. debug.traceback(), obj)
end

function Debug.LogError(msg, obj)
    Util.LogErrorEx(msg .. "\n" .. debug.traceback(), obj)
end

--返回src, linedefined
function Debug.GetLuaFunctionInfo(func)
    local info = debug.getinfo(func)

    local linedefined = -1
    local src = ""

    -- for k,v in pairs(info) do
    -- 	Debug.LogWarning(">> " .. k .. "  " .. tostring(v));
    -- end
    if (info["source"]) then
        src = info["source"]
    end

    if (info["linedefined"]) then
        linedefined = info["linedefined"]
    end

    return src, linedefined
end

local printFunc
function goprint(...)
    local arg = {...}
    if
        UnityEngine.Application.platform == UnityEngine.RuntimePlatform.OSXEditor or
            UnityEngine.Application.platform == UnityEngine.RuntimePlatform.WindowsEditor
     then
        local str = table.concat(arg, "  ")
        if not printFunc then
            printFunc = print
        end
        printFunc(debug.traceback(str))
    end
end
----------------------Debug Extend Method End

----------------------C Sharp String Tool Start
CSString = CSString or {}
--cstolua在Wrap层处理了各种基本类型从lua转换为c#类型，
--以及对一些类型的返回结果也经过了转换，所以直接传lua参数给Wrap的c#方法而不用作转换
function CSString.Format(...)
    if (select("#", ...) == 1) then
        return System.String.Format(..., 0)
    else
        return System.String.Format(...)
    end
end

function CSString.Substring(text, i, e)
    local str = System.String.New(tostring(text))
    return str:Substring(i, e)
end
function CSString.SubstringAll(text, i)
    local str = System.String.New(tostring(text))
    return str:Substring(i)
end

function CSString.Length(text)
    local str = System.String.New(tostring(text))
    return str.Length
end

----------------------C Sharp String Tool End

----------------------Lua String Tool Start
LuaString = LuaString or {}
LuaString.re = require("lpeg/re")

--有问题，不要用
--仅支持最基本的{0}，
--不支持高级表达式诸如{0:xxx}之类，
--也不支持{{和}}这类用法
--从{0}开始计数
function LuaString.Format(...)
    local paramCount = select("#", ...)
    if (paramCount > 0) then
        local exp = select(1, ...)
        if (type(exp) == "string") then
            if (paramCount == 1) then
                return exp
            else
                --似乎工程里有LPeg的库和封装，就直接拿过来用了
                local re = LuaString.re
                for i = 2, paramCount do
                    local tmpParam = select(i, ...)
                    if (tmpParam) then
                        --todo : 如果有高级样式，应该回滚调用CSString.Format
                        exp = re.gsub(exp, "({[" .. (i - 2) .. "]})", tmpParam)
                    else
                        error("LuaString.Format: param in index " .. i .. "is nil")
                        exp = re.gsub(exp, "({[" .. (i - 2) .. "]})", "")
                    end
                end
                return exp
            end
        else
            error("LuaString.Format: param in index 1 is not string")
            return nil
        end
    else
        error("LuaString.Format: no param")
        return nil
    end
end

----------------------Lua String Tool End

local next = next --more efficient to accessing the method
-- 判断Table是否是空的或者nil
function IsTableEmptyOrNil(t)
    return t == nil or next(t) == nil
end
-- 判断Table是否是空的,如果传入的是nil，返回false，请自行提前判断是否为nil
function IsTableEmpty(t)
    return t ~= nil and next(t) == nil
end
-----------------------------------------------Lua 分割字符串------------------------------------------------------
function LuaSplit(s, symbol)
    local words = {}
    local length = string.len(s)
    local stringS = ""
    for i = 1, length do
        if string.sub(s, i, i) == symbol then
            table.insert(words, stringS)
            stringS = ""
        else
            stringS = string.format("%s%s", stringS, string.sub(s, i, i))
        end
        if i == length then
            table.insert(words, stringS)
        end
    end
    return words
end

--@param time mSecond
function timeStampTransfer(time)
    if time > 0 then
        local leftTime = time
        local day = math.floor(leftTime / 3600 / 24)
        leftTime = leftTime - day * 3600 * 24
        local hour = math.floor(leftTime / 3600)
        leftTime = leftTime - hour * 3600
        local minite = math.floor(leftTime / 60)
        leftTime = leftTime - minite * 60
        local second = math.floor(leftTime % 60)

        if day < 0 then
            day = 0
        end
        if hour < 0 then
            hour = 0
        end
        if minite < 0 then
            minite = 0
        end
        if second < 0 then
            second = 0
        end
        return day, hour, minite, second
    else
        return 0, 0, 0, 0
    end
end

function sortKeyByAlphabetic(t, f)
    local a = {}
    for n in pairs(t) do
        table.insert(a, n)
    end
    table.sort(a, f)
    local i = 0 -- iterator variable
    local iter = function()
        -- iterator function
        i = i + 1
        if a[i] == nil then
            return nil
        else
            return a[i], t[a[i]]
        end
    end
    return iter
end

--冒泡排序
function BubbleSort(arr)
    for i = 1, #arr do
        for j = 1, #arr - i do
            if arr[j] > arr[j + 1] then
                local temp = arr[j]
                arr[j] = arr[j + 1]
                arr[j + 1] = temp
            end
        end
    end
end

---解析固化奖励表数据
function AnalReward(rewardId)
    local rewardData = DataManager.GetDataEntity(GameLogic.rewardData, rewardId);
    local id = {};
    local type = {};
    local num = {};
    for i = 1, 4 do
        local RewardType = rewardData[string.format("Reward%dType", i)];
        local RewardID = rewardData[string.format("Reward%dID", i)];
        local RewardNum = rewardData[string.format("Reward%dNum", i)];
        if RewardType ~= 0 then
            table.insert(id, RewardID);
            table.insert(type, RewardType);
            table.insert(num, RewardNum);
        end
    end
    local reward = {};
    reward.id = id;
    reward.type = type;
    reward.num = num;
    return reward;
end