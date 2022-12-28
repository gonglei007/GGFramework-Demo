require("Game.Lobby.HelpUI.HelpWindowView")

HelpWindowCtrl = class("HelpWindowCtrl", WindowController)

local instance = nil

function HelpWindowCtrl:ctor()

end

function HelpWindowCtrl.Instance()
    if instance == nil then
        instance = HelpWindowCtrl.new()
    end
    return instance
end

function HelpWindowCtrl:OnViewLoad()
    log("OnViewLoad")
end

function HelpWindowCtrl:ShowHelp(textKey)
    self.super.ShowWindow(self)
    self.view:Refresh(textKey)
end

function HelpWindowCtrl:HideWindow()
    self.super.HideWindow(self)
end

