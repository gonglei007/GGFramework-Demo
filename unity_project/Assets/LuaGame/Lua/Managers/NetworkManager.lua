
cjson = require("cjson")
require("Game.Logic.Player");

NetworkManager = {}
local this = NetworkManager;
this.isReLogin = false;
local is_in_game = false;

local gameServerName = "game";
local notifyServerName = "notify";

function NetworkManager.Init(host, port, token, callback)
    this.host = host;
    this.port = port;
    this.token = token;

    ---TODO: GL - 连接服务器，如果连接失败，弹出消息框，不能继续。
    print("准备连接服务器:"..host);
    print("准备连接服务器:"..port);
    NetworkManager.ConnectGameServer(host, port, function(msg)
            print("On Connected:".. tostring(msg));
            callback();
        end);
end

local gameClient = nil;
function NetworkManager.ConnectGameServer(host, port, callback)
    gameClient = networkSystem:ConnectNetworkClient(gameServerName, host, port, LuaHelper.ActionParam(function(msg)
        print("Game server on Connected:".. tostring(msg));
        gameClient:On("server.push.msg", LuaHelper.ActionParam(function(message)
            NetworkManager.HandleGameServerNotify(message);
        end));
        callback(msg);
    end));
end

function NetworkManager.CloseGameServer()
    if gameClient ~= nil then
        gameClient:Close();
    end
end

function NetworkManager.HandleGameServerNotify(message)
    --TODO: GL -
    print("==========>HandleGameServerNotify:"..message);
    --这里是服务器推送消息处理
    local messageObject = cjson.decode(tostring(message));
    if messageObject.response ~= nil then
        local str = CommonTools.Unzip(messageObject.response);
        str = string.gsub(str, "\\", "");
        messageObject = cjson.decode(str);
    end
    if messageObject.event ~= nil then
        -- NetworkManager.ProcessServerPush(messageObject);
        CardNetWorkManager.ProcessServerPush(messageObject);
    end
end

local notifyClient = nil;
function NetworkManager.ConnectNotifyServer(callback)
    local serviceInfo = networkSystem:GetServiceInfo("gateway");
    print("准备通知服务！"..serviceInfo.host..":"..serviceInfo.port);
    notifyClient = networkSystem:ConnectNetworkClient(notifyServerName, serviceInfo.host, serviceInfo.port, LuaHelper.ActionParam(function(msg)
        print("Notify server on Connected:".. tostring(msg));
        local message = {};
        message.uid = Player.uid;
        networkSystem:SendRequestX(notifyServerName, "connector.entryHandler.initSession", cjson.encode(message), function(response)
            print("初始化通知服seesion完毕！");
            local responseObject = cjson.decode(tostring(response));
            local code = tonumber(responseObject["code"]);
            if (200 == code) then
                print("2");
                notifyClient:On("server.gateway.kickUserOffline", LuaHelper.ActionParam(function(message)
                    NetworkManager.HandleNotifyServerKickUserOffline(message);
                end));
                notifyClient:On("server.game.boardFinishReward", LuaHelper.ActionParam(function(message)
                    NetworkManager.HandleNotifyServerBoardFinishReward(message);
                end));
                notifyClient:On("server.lobby.payDone", LuaHelper.ActionParam(function(message)
                    log("server.lobby.payDone！");
                    NetworkManager.PayDonePush(message);
                end));
                --notifyClient:On("server.match.matchOK", LuaHelper.ActionParam(function(message)
                --    NetworkManager.MatchOKFinish(message);
                --end));
                notifyClient:On("server.push.msg", LuaHelper.ActionParam(function(message)
                    NetworkManager.PushParse(message);
                end));
                callback(msg);
            else
                uiSystem:PopDialogX(TextSystem.GetText("error"), responseObject["msg"].ToString(), UIManager.DialogOption["Yes"], function(option)
                    callback(responseObject["msg"].ToString());
                end);
            end
        end);
    end));
end

---推送解析总入口
function NetworkManager.PushParse(pushData)
    print("==========>PushParse:"..pushData);
    local data = cjson.decode(pushData);
    local pushEvent = data["event"];
end

function NetworkManager.PayDonePush(message)
    local messageObject = cjson.decode(tostring(message));
    log("PayDonePush" .. cjson.encode(messageObject["reward"]));
    local code = messageObject["code"];
    if (code == NetworkConst.CODE_OK) then
        local reward = messageObject["reward"];
        RewardManager.ReceiveItems(reward);
        ShopUICtrl.Instance():RefreshShopData();
    end
end

function NetworkManager.HandleNotifyServerBoardFinishReward(message)
    local messageObject = cjson.decode(tostring(message));
    local code = messageObject["code"];
    if (code == NetworkConst.CODE_OK) then
        local reward = messageObject["reward"];
    end
end

--服务器主动推送的踢掉玩家的消息
function NetworkManager.HandleNotifyServerKickUserOffline(message)
    --TODO: GL -
    print("==========>HandleNotifyServerKickUserOffline:"..message);
    networkSystem:CloseAll();
    uiSystem:PopDialogX(TextSystem.GetText("be_kicked"), response["msg"].ToString(), UIManager.DialogOption["Yes"], function(option)
        Application.Quit();
    end);
end

function NetworkManager.SendRequestToGameServer(route, message, callback)
    networkSystem:SendRequestX(gameServerName, route, cjson.encode(message), LuaHelper.ActionParam(callback));
end

function NetworkManager.SendRequestToNotifyServer(route, message, callback)
    networkSystem:SendRequestX(notifyServerName, route, cjson.encode(message), LuaHelper.ActionParam(callback));
end

---
---进入游戏服务器
---将服务器传来数据传回给服务器，用来支持服务器架构引发的查找数据困难的问题
function NetworkManager.EnterGame(callback)
    local msg = {}
    if Player.uid == nil then
        Player.uid = UnityEngine.Random.Range(10002, 40000);
    end
    msg.uid = Player.uid;
    if Player.nick == nil then
        Player.nick = tostring(Player.uid);
    end
    msg.nick = tostring(Player.nick);
    msg.token = this.token;
    msg.serverKey = "";
    msg.apValue = 0;
    msg.headIcon = Player.headIcon;
    msg.isReLogin = this.isReLogin;
    this.isReLogin = true;
    for k, v in pairs(LobbyModel.enterGameData) do
        msg[k] = v;
    end

    NetworkManager.SendRequestToGameServer("connector.entryHandler.enterGame", msg,callback);
end

function NetworkManager.Unload()
    networkSystem:CloseClient("game");
end

return NetworkManager;