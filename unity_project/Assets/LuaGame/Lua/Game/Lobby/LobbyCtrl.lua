---
--- Created by User.
--- DateTime: 2019-08-14 15:24
---
local math = require("math")
require("Game.Lobby.LobbyView");

LobbyCtrl = class("LobbyCtrl", PageController)

local instance;

function LobbyCtrl:ctor()
    self.super:ctor();
end

function LobbyCtrl:OnViewLoad()
    Debug.LogWarning("Lobby加载完成。view name:"..tostring(self.view.buttonDemo == nil));
    EventDelegate.Add(self.view.buttonDemo.onClick, EventDelegate.Callback(function() self:OnEnterDemoClick(); end));
    self:Reset();
end

function LobbyCtrl.Instance()
    if instance == nil then
        instance = LobbyCtrl.new();
    end
    return instance;
end

function LobbyCtrl:OpenPage(hideCurrentPage)
    self:Present(hideCurrentPage);
    LobbyCtrl.ShowUpgrade();
end

function LobbyCtrl:Reset()

end

---页面显示前的回调
function LobbyCtrl:OnPreShow()
    --soundSystem:PlayBGM("bgm_world");
    --清除全部消息
    MessageCenter.ClearEvents();
    --注册全部消息
    LobbyCtrl.InitAllEvents();
    --gameSystem:ChangeState(GameSystem.States.Lobby);
end

---页面显示后的回调
function LobbyCtrl:OnShowed()
end

function LobbyCtrl:InitEvents()

end

--注册全部消息
function LobbyCtrl.InitAllEvents()
    LobbyCtrl.Instance():InitEvents();
end

---
--- 获取大厅数据。
---
function LobbyCtrl:EnterLobby(callback)
    callback(nil);
end

function LobbyCtrl:OnEnterDemoClick()
    stageSystem.Instance:ChangeStage("demo");
end

function LobbyCtrl:OnDestroy()
    self = nil;
end


