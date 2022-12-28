--[[
--Created Date: Tuesday, November 21st 2017, 5:33:37 pm
--Author: Yu GuoPing (ccwigg@qq.com)
--Copyright © 2017 - 2017, GoTech.Co.Ltd
--]]

PrefabTool = {};
--itemList:需要绑定gameObject
function PrefabTool.DestroyPrefabs(itemList)
    if (itemList == nil) then
        return
    end
    for i = 1, #itemList do
        GameObject.Destroy(itemList[i].gameObject);
        itemList[i] = nil;
    end

end

--ControllerClass需要实现InitPanel(gameObject)方法
function PrefabTool.AddTempNewPrefabItems(targetCount, targetTrans, prefabObj, ControllerClass)
    if targetCount <= 0 then
        return nil;
    else
        local list = {};

        for i = 1, targetCount do
            local tmpObj = NGUITools.AddChild(targetTrans.gameObject, prefabObj);
            if (ControllerClass) then
                local controller = ControllerClass.new();
                controller:InitPanel(tmpObj);
    
                table.insert(list, controller);
            else
                table.insert(list, tmpObj);
            end

        end

        return list;
    end
end

function PrefabTool.AddNewPrefabItem(targetTrans, usedItemList, recycledItemList, prefabObj, ControllerClass)
    if (#recycledItemList > 0) then
        local tmpItem = recycledItemList[#recycledItemList];
        table.remove(recycledItemList);
        tmpItem.transform.parent = targetTrans;
        table.insert(usedItemList, tmpItem);
    else
        local tmpObj = NGUITools.AddChild(targetTrans.gameObject, prefabObj);
        if (ControllerClass) then
            local controller = ControllerClass.new();---此处:New改为.New  适应小三杀系统
            controller:InitPanel(tmpObj);

            table.insert(usedItemList, controller);
        else
            table.insert(usedItemList, tmpObj);
        end
    end
end

--大量预制体出现在同一个坐标上会对NGUI渲染产生问题 把储存的容器设置成关闭或者调整他渲染的面积（UIPanel）
function PrefabTool.RecyclePrefabItem(recycleTrans, usedItemList, recycledItemList)
    recycleTrans.gameObject:SetActive(false);
    if (#usedItemList > 0) then
        local tmpItem = usedItemList[#usedItemList];
        table.remove(usedItemList);
        table.insert(recycledItemList, tmpItem);
        tmpItem.transform.parent = recycleTrans;
        tmpItem.transform.localPosition = Vector3.New();
    end
end

function PrefabTool.RecycleAllPrefabItem(recycleTrans, usedItemList, recycledItemList)
    local length = #usedItemList;

    for i = 1, length do
        PrefabTool.RecyclePrefabItem(recycleTrans, usedItemList, recycledItemList);
    end
end

function PrefabTool.CachePrefabItem(recycleTrans, recycledItemList, prefabObj, cacheCount, ControllerClass)
    for i = 1, cacheCount do
        local tmpObj = NGUITools.AddChild(recycleTrans.gameObject, prefabObj);
        if (ControllerClass) then
            local controller = ControllerClass.new();
            controller:InitPanel(tmpObj);
            table.insert(recycledItemList, controller);
        else
            table.insert(recycledItemList, tmpObj);
        end
    end
end

function PrefabTool.AdjustPrefabItemCount(targetCount, recycleTrans, targetTrans, usedItemList, recycledItemList, prefabObj, ControllerClass)
    local delta = targetCount - #usedItemList;
    if (delta > 0) then
        for i = 1, delta do
            PrefabTool.AddNewPrefabItem(targetTrans, usedItemList, recycledItemList, prefabObj, ControllerClass);
        end
    else
        for i = 1, -delta do
            PrefabTool.RecyclePrefabItem(recycleTrans, usedItemList, recycledItemList);
        end
    end
end

--一些场景可能会存留一些在编辑期使用而未删除的Prefab(或者你压根就不想从bundle里去动态加载这些prefab)，
--这个方法将会收集指定节点下的这类prefab，并存入回收池中（recycledItemList, recycleTrans）
--match: 一个简单的匹配用字符串，当不为nil时，指定节点下的物体名字中若包含这个字符串，该物体才会被处理；当为nil时则不作这个判断
function PrefabTool.CollectFreePrefabs(targetTrans, recycleTrans, recycledItemList, ControllerClass, match)
    if (targetTrans and recycledItemList) then
        local childCount = targetTrans.childCount;
        local tmpArr = {};
        for i = 0, (childCount - 1) do
            local tmpObj = targetTrans:GetChild(i).gameObject;
            local pass = true;
            if (match) then
                if (not string.find(tmpObj.name, match)) then
                    pass = false;
                end
            end
            if (pass) then
                if (ControllerClass) then
                    local controller = ControllerClass.new();
                    controller:InitPanel(tmpObj);
                    table.insert(recycledItemList, controller);
                else
                    table.insert(recycledItemList, tmpObj);
                end
                table.insert(tmpArr, tmpObj);
            end
        end

        --在遍历完成后再挪动对应的子物体，否则当挪动子物体后targetTrans:GetChild(i)得到的结果有问题
        for i = 1, #tmpArr do
            tmpArr[i].transform.parent = recycleTrans;
            tmpArr[i].transform.localPosition = Vector3.New();
        end
    end
end

-- 释放pItemList中的各item所持有的资源，并清空pItemList
function PrefabTool.ReleaseAndClearItemList(pItemList)
    if (pItemList ~= nil) then
        for k, v in pairs(pItemList) do
            if (v.ReleasePanel) then
               v:ReleasePanel();
            elseif (v._ReleaseComponent) then
                -- 有些脚本不是从新模版生成的，可能只有_ReleaseComponent而没有ReleasePanel
               v:_ReleaseComponent();
            end
            pItemList[k] = nil;
        end
    end
end

-- 释放pItemList中的各item所持有的资源
function PrefabTool.ReleaseItemList(pItemList)
    if (pItemList ~= nil) then
        for k, v in pairs(pItemList) do
            if (v.ReleasePanel) then
               v:ReleasePanel();
            elseif (v._ReleaseComponent) then
                -- 有些脚本不是从新模版生成的，可能只有_ReleaseComponent而没有ReleasePanel
               v:_ReleaseComponent();
            end
        end
    end
end