HelpWindowView = class("HelpWindowView",WindowView)

local instance = nil

function HelpWindowView:ctor()
    self.super.ctor(self)
    self.name = "HelpWindow"
end

function HelpWindowView.Instance()
    if instance == nil then
        instance = HelpWindowView.new();
    end
    return instance;
end

function HelpWindowView:Load()
    self.super.Load(self)
end

function HelpWindowView:OnViewLoad()
    self.super.OnViewLoad()
    self:FindComponent()
end

function HelpWindowView:FindComponent()
    self.transform = self.transform;
    self.HelpPanel = self.transform:Find("Panel").gameObject --暂时没用
    self.contentView = self.transform:Find("Panel/ContentView"):GetComponent("UIScrollView")
    self.contentText = self.transform:Find("Panel/ContentView/ContentText"):GetComponent("UILabel")

    local mask = self.transform:Find("Mask"):GetComponent("UIEventTrigger")
    mask.onClick:Add(EventDelegate(function()
        self:CloseFunc();
    end))
end

function HelpWindowView:Refresh(textKey)
    self.contentText.text = TextSystem.GetText(textKey);
    self.contentView:ResetPosition();

    self.HelpPanel.transform.localScale = Vector3.zero
    --动画缩放
    self.HelpPanel.transform:DOScale(Vector3.one,0.2):OnComplete(function()
    end)
end

--关闭
function HelpWindowView:CloseFunc()
    HelpWindowCtrl.Instance():HideWindow()
    if self.closeCallback then --关闭回调
        self:closeCallback()
        self.closeCallback = nil
    end
end
