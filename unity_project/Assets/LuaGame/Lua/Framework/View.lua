---
--- Created by GL.
--- DateTime: 2019-08-14 15:43
---

View = class("View")

local entity = nil;
function View:ctor(e)
    --Debug.LogWarning("####->View:ctor->"..tostring(e).." | "..tostring(self));
    entity = self;
    self.name = nil;
end

function View:GetName()
    return self.name;
end

-- 各个子View从Page获取所需的部件，存入self。
function View:Load(callback)
    --Debug.LogWarning("3.####->View:Load->"..tostring(entity));
    self:OnViewLoad();
    --entity:OnViewLoad();
    if callback ~= nil then
        callback();
    end
end

function View:OnViewLoad()
    --Debug.LogWarning("5.####->View:OnViewLoad->"..tostring(entity).." |"..self.name);
    --print("调用了父类的 OnViewLoaded");
end

function View:OnPreShow()

end

function View:OnShowed()

end

function View:OnShow()

end
