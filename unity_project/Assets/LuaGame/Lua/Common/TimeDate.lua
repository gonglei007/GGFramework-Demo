---时间类

TimeDate = {};
--print(os.time());
--print(os.date())
--print(os.date("%x", os.time()))
--print(os.date("%X", os.time()))
--print(os.date(%Y-%m-%d%H:%M:%S", os.time()))


--当前日期24點（8位数字表示的年月日，如20210521）转换成毫秒
function TimeDate.DateEndToMS(timeLog)
    if CSString.Length(timeLog) >= 8 then
        local year = CSString.Substring(timeLog,0,4);
        local month =  CSString.Substring(timeLog,4,2);
        local day =  CSString.Substring(timeLog,6,2) + 1;--日期加1即第二天零點
        local time = {year = year, month = month, day = day, hour = 0, min = 0, sec = 0}
        return os.time(time) * 1000;
    else
        return nil;
    end
end

--当前日期零點（8位数字表示的年月日，如20210521）转换成毫秒
function TimeDate.DateBeginToMS(timeLog)
    if CSString.Length(timeLog) >= 8 then
        local year = CSString.Substring(timeLog,0,4);
        local month =  CSString.Substring(timeLog,4,2);
        local day =  CSString.Substring(timeLog,6,2);
        local time = {year = year, month = month, day = day, hour = 0, min = 0, sec = 0}
        return os.time(time) * 1000;
    else
        return nil;
    end
end

---当前時間點是否在规定时间(8位数字表示的年月日，如20210521)范围内
---返回值同TimeDate.AnyTimeInRange
function TimeDate.CurrentTimeInRange(startT, endT)
    return TimeDate.AnyTimeInRange(startT, endT, GameLogic.CurrentServerTimeSecond());
end

---startTimeMS, endTimeMS, time：單位：毫秒
---返回值同TimeDate.AnyTimeInRangeMS
function TimeDate.CurrentTimeInRangeMS(startTimeMS, endTimeMS)
    return TimeDate.AnyTimeInRangeMS(startTimeMS, endTimeMS, GameLogic.CurrentServerTimeSecond());
end

---任意时间點是否在规定时间(8位数字表示的年月日，如20210521)范围内
---time：單位：毫秒
---startT，endT：8位数字表示的年月日，如20210521
---返回值同TimeDate.AnyTimeInRangeMS
function TimeDate.AnyTimeInRange(startT, endT, time)
    local startTimeMS = TimeDate.DateBeginToMS(startT);
    local endTimeMS = TimeDate.DateBeginToMS(endT);
    local nowTimeMS = tonumber(tostring(time))
    TimeDate.AnyTimeInRangeMS(startTimeMS, endTimeMS, nowTimeMS);
end

---startTimeMS, endTimeMS, time：單位：毫秒
---return：0:表示在范围内，-1：表示未到开始时间点，1：表示超过结束时间点；-2：数据不合法
function TimeDate.AnyTimeInRangeMS(startTimeMS, endTimeMS, time)
    local nowTime = tonumber(tostring(time))
    if startTimeMS ~= nil and endTimeMS ~= nil  then
        if (nowTime < startTimeMS) then
            return -1;
        elseif (nowTime > endTimeMS) then
            return 1;
        else
            return 0;
        end
    else
        return -2;
    end
end

---任意两个时间點比较大小(8位数字表示的年月日，如20210521)
---time1，time2：8位数字表示的年月日，如20210521

function TimeDate.TimeCompare(time1, time2)
    local timeMS1 = TimeDate.DateBeginToMS(time1);
    local timeMS2 = TimeDate.DateBeginToMS(time2);
    return TimeDate.TimeCompareMS(timeMS1, timeMS2);
end

---任意两个时间點比较大小，参数单位毫秒
---return：0:表示相等，-1：表示time1小，1：表示time1大；-2：数据不合法
function TimeDate.TimeCompareMS(timeMS1, timeMS2)
    if timeMS1 ~= nil and timeMS2 ~= nil then
        if (timeMS1 < timeMS2) then
            return -1;
        elseif (timeMS1 > timeMS2) then
            return 1;
        else
            return 0;
        end
    else
        return -2;
    end
end

-- 转化策划表中的时间格式为时间戳 默认返回时间戳
-- eg：unixStamp 2018-11-09 00:00:00 -> 212412111542
-- eg: table {year, month, day, hour, minite, second} 按顺序
function TimeDate.FormatTableTimeToTimeStamp(time, returnType)
    local timeStr = {}
    local j = 0;
    for w in string.gmatch(time, "%d+") do
        j = j + 1;
        timeStr[j] = w;
    end

    local unixStamp = os.time({
        year = timeStr[1],
        month = timeStr[2],
        day = timeStr[3],
        hour = timeStr[4],
        minute = timeStr[5],
        second = timeStr[6]})
    if returnType == "timeStamp" or returnType == nil then
        return unixStamp;
    elseif returnType == "timeTable" then
        return timeStr;
    end
end

function TimeDate.TimeRangeComporForLong(starT,endT,comporTime)
    if starT ~= nil and endT ~= nil  then
        local nowTime = tonumber(tostring(comporTime))
        if starT ~= nil and endT ~= nil  then
            if nowTime >= starT and nowTime <= endT then
                return true;
            else
                return false;
            end
        end
    end
end
--计算当天当前时间是否在开启时间段的范围 参数（开始的小时，结束的小时）仅限小时
function TimeDate.TimeRangeHour(starHor,endHor)
    local severTime = GameLogic.CurrentServerTimeSecond();
    local nowTime = os.date("*t",severTime);
    local nowHorTime = nowTime.hour + nowTime.min / 60;
    local result = false;
    if tonumber(nowHorTime) >= tonumber(starHor) and tonumber(nowHorTime) < tonumber(endHor)  then
        result = true;
    end
    return result;
end


function TimeDate.Year()
    local nowTime = GameLogic.CurrentServerTimeSecond();
    return os.date("%Y", nowTime);
end
function TimeDate.Mon()
    local nowTime = GameLogic.CurrentServerTimeSecond();
    return os.date("%m", nowTime);
end
function TimeDate.Day()
    local nowTime = GameLogic.CurrentServerTimeSecond();
    return os.date("%d", nowTime);
end

function TimeDate.Now()
    return  os.date("%Y-%m-%d %H:%M:%S");
end


--有参数时候 没参数时候 不解释了
function TimeDate.Hour(time, severTime)
    if time ~= nil then
        local nowTime = os.date("*t",tonumber(severTime));
        local inTime = os.date("*t",tonumber(time));
        local nowhour = nowTime.yday *24 + nowTime.hour;
        local intimeHour = inTime.yday * 24 + inTime.hour;
        local timeOut = tonumber(nowhour) - tonumber(intimeHour);
        return timeOut;
    end
end

--截止今日24点还剩下的时间。格式：xx时xx分
function TimeDate.TodayLeftTime()
    local nowTime = os.date("*t", tonumber(GameLogic.CurrentServerTimeSecond()));
    local leftHour = 23 - nowTime.hour;
    local leftMin = 60 - nowTime.min;

    return "" .. leftHour .. TextSystem.GetText("hour") .. leftMin .. TextSystem.GetText("minute");
end

function TimeDate.CurrentOSTime()
    return os.date("*t", tonumber(GameLogic.CurrentServerTimeSecond()));
end

--计算指定时间和当前时间的天数差距 当前服务器时间的24点   例如今天8月5日0点和8月4日23点 相差1天
function TimeDate.DisTanceToday(time)
    --local t = {year = 2019, month = 01, day = 01, hour = 0,min = 0, sec = 0}
    --local ceT = os.time(t);
    local date = GameLogic.CurrentServerTimeSecond();--服务器时间转化成 lua里面的表
    local nowTime = os.date("*t", date);--lua 转化成DataTime(c#)
    local yday = nowTime.yday; --现在时间距离今年过去了多少天 363
    local GreatTime = os.date("*t", tonumber(tostring(time))/1000);-- 传进来的时间转化成DataTime
    local greatYday = GreatTime.yday;--注册时间的那一年 过了多少天
    if (tonumber(nowTime.year) == tonumber(GreatTime.year)) then--同一年
        return yday - greatYday;
    else--不是同一年
        local day = 0;
        if (tonumber(GreatTime.year+1) < tonumber(nowTime.year)) then
            --虽然此情况不可能有
            local disYear = tonumber(nowTime.year) - tonumber(GreatTime.year);
            day = day + tonumber(nowTime.yday);
            if (tonumber(GreatTime.year) % 4 == 0) then
                day = day + (366 - tonumber(GreatTime.yday));
            else
                day = day + (365 - tonumber(GreatTime.yday));
            end
            if disYear >=2 then
                disYear = disYear -1;
                local y = tonumber(GreatTime.year);
                for i = 1, disYear do
                    y = y+1;
                    if (y % 4 == 0) then
                        day = day + 366;
                    else
                        day = day +365;
                    end
                end
            end
            return day;
        else
            if (tonumber(GreatTime.year) % 4 == 0) then
                day = day + (366 - tonumber(GreatTime.yday));
            else
                day = day + (365 - tonumber(GreatTime.yday));
            end
            day = day + tonumber(nowTime.yday);
        end
        return day;
    end
end


