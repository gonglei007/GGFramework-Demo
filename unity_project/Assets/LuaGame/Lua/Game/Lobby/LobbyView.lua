--------
---主入口view
--------


LobbyView = class("LobbyView", PageView)
local this = LobbyView;
local _Instance = nil;

function LobbyView:ctor()
    LobbyView.super:ctor(self);
    self.name = "LobbyPage";
end

function LobbyView.Instance()
    if _Instance == nil then
        _Instance = LobbyView.new();
    end
    return _Instance;
end

function LobbyView:OnViewLoad()
    LobbyView.super.OnViewLoad(self);
    self:FindComponent()

    ---加载后需要做什么写在这里
end

function LobbyView:FindComponent()
    Debug.LogWarning("LobbyView开始绑定部件!")
    self.buttonDemo = Util.FindChildEx(self.transform,"LobbyButtons/buttonDemo"):GetComponent("UIButton");
    --Debug.LogWarning("buttonDemo:" .. tostring(self.buttonDemo == nil));
end

--- 做本页面的动画表现。
function LobbyView:OnShow()
    LobbyView.super.OnShow(self);

end
