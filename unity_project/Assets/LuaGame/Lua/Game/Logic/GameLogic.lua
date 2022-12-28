require("Game.Logic.Resource.ResManager")
require("Game.Logic.Hero.HeroManager")
require("Game.Logic.Chest.ChestManager")
require("Game.Logic.Prop.PropManager")
require("Game.Logic.Card.CardManager")
require("Game.Logic.Enum")
require("Game.Logic.GuideManager")

GameLogic = class("GameLogic");
local instance = nil;
local this = GameLogic;

GameLogic.startServerTime = 0;
GameLogic.startLocalTime = 0;
GameLogic.playerLevelDataTableName = "PlayerLevelData";         ---玩家升级表
GameLogic.headFrameDataTableName = "HeadFrameData";             ---头像框表
GameLogic.heroDataTableName = "HeroData";                       ---英雄表
---heroCardDataTableName与cardDataTableName相同是因为表在重复修改，所以先改成相同的
GameLogic.heroCardDataTableName = "DuelCardData";
GameLogic.cardDataTableName = "DuelCardData";
GameLogic.chestDataTableName = "ChestData";                     ---寶箱表
GameLogic.propDataTableName = "PropData";                       ---道具表
GameLogic.resourceDataTableName = "ResourceData";               ---資源表
GameLogic.redbagDataTableName = "RedbagData";                   ---紅包表
GameLogic.chatbubbleDataTableName = "ChatbubbleData";           ---聊天气泡表
GameLogic.powerScoreData = "PowerScoreData";                    ---战力计算表
GameLogic.functionUnlockName = "UnlockData";                    ---功能解锁表
GameLogic.loginBonusData = "LoginBonusData";                    ---七日登陆奖励
GameLogic.rewardData = "RewardData";                            ---七日登陆奖励
GameLogic.dailyVictoryRewardData = "DailyVictoryRewardData"     ---天梯累胜奖励

function GameLogic:ctor()

end

---单例，方便外界调用
function GameLogic.Instance()
    if instance == nil then
        instance = GameLogic.new();
    end
    return instance;
end

function GameLogic.Init()
    --初始化函数
    ResManager.Init();
    PropManager.Init();
    HeroManager.InitHeroData();
    ChestManager.Init();
    CardManager.Init();
end

function GameLogic.GetItemName(type, id)
    if type == ItemType.Resource then      --物资
        return ResManager.GetItemName(id);
    end

    if type == ItemType.Hero then      --武将
        log("GameLogic.GetItemName hero id: "..id);
        return HeroManager.GetHero(id).name;
    end

    if type == ItemType.Gold then      --游戏币
        return TextSystem.GetText(string.format("gold"));
    end

    if type == ItemType.Diamond then     --元宝（虚拟币）
        return TextSystem.GetText(string.format("diamond"));
    end

    if type == ItemType.Card then     --卡牌
        return CardManager.GetItemName(id);
    end

    if type == ItemType.Exp then     --玩家经验
        return TextSystem.GetText("exp");
    end

    if type == ItemType.Prop then   --道具
        return PropManager.GetItemName(id);
    end

    if type == ItemType.Chest then   --宝箱
        return ChestManager.GetItemName(id);
    end

    if type == ItemType.HeadFrame then   --頭像框
        return TextSystem.GetText(string.format("frame_name_%s", id));
    end

    if type == ItemType.ChatBubble then   --聊天氣泡
        return TextSystem.GetText(string.format("chat_bubble_%d", id));
    end
end

function GameLogic.GetItemDescription(type, id)
    if (type == ItemType.Resource) then      --物资
        return TextSystem.GetText(string.format("resource_description_%d", id));
    elseif (type == ItemType.Hero) then      --武将
        return TextSystem.GetText(string.format("HeroCardDetail_%d", id));
    elseif (type == ItemType.Gold) then      --金幣
        return TextSystem.GetText("gold_description");
    elseif (type == ItemType.Diamond) then      --鑽石
        return TextSystem.GetText("diamond_description");
    elseif (type == ItemType.Card) then     --卡牌
        return TextSystem.GetText(string.format("DuelCardDetail_%d", id));
    elseif (type == ItemType.Exp) then     --卡牌
        return TextSystem.GetText("exp_description");
    elseif (type == ItemType.Prop) then      --道具
        return TextSystem.GetText(string.format("prop_description_%d", id));
    elseif (type == ItemType.Chest) then      --宝箱
        return TextSystem.GetText(string.format("chest_description_%d", id));
    elseif type == ItemType.HeadFrame then   --頭像框
        return TextSystem.GetText(string.format("frame_des_%d", id));
    elseif type == ItemType.ChatBubble then  --聊天氣泡
        return TextSystem.GetText(string.format("chat_bubble_description_%d", id));
    else
        return "";
    end
end

function GameLogic.GetItemSpriteName(type, id)
    if (type == ItemType.Resource) then     --物资
        return DataManager.GetDataEntity(GameLogic.resourceDataTableName, id).Image;
        --return resources[tonumber(id)].data.Image;
    elseif (type == ItemType.Hero) then     --武将
        return DataManager.GetDataEntity(GameLogic.heroDataTableName, id).Image;
    elseif (type == ItemType.Card) then     --卡牌
        return CardManager.GetTextureName(id);
    elseif (type == ItemType.Prop) then     --道具
        return DataManager.GetDataEntity(GameLogic.propDataTableName, id).Image;
    elseif (type == ItemType.Chest) then    --宝箱
        return DataManager.GetDataEntity(GameLogic.chestDataTableName, id).Image;
    elseif type == ItemType.HeadFrame then   --頭像框
        return DataManager.GetDataEntity(GameLogic.headFrameDataTableName, id).Image;
    elseif type == ItemType.ChatBubble then  --聊天氣泡
        return DataManager.GetDataEntity(GameLogic.chatbubbleDataTableName, id).Image;
    elseif (type == ItemType.Gold) then     --金币
        return "gold";
    elseif (type == ItemType.Diamond) then  --元宝
        return "diamond";
    elseif (type == ItemType.Exp) then      --经验
        return "exp";
    else
        return " ";
    end
end

function GameLogic.GetChestAnimSkinName(type, id)
    if (type == ItemType.Chest) then    --宝箱
        return DataManager.GetDataEntity(GameLogic.chestDataTableName, id).AnimSkin;
    end
end

function GameLogic.GetItemQuality(type, id)
    if (type == ItemType.Resource) then     --物资
        return DataManager.GetDataEntity(GameLogic.resourceDataTableName, id).Quality;
    elseif (type == ItemType.Hero) then     --武将
        return DataManager.GetDataEntity(GameLogic.heroDataTableName, id).Quality;
    elseif (type == ItemType.Prop) then     --道具
        return DataManager.GetDataEntity(GameLogic.propDataTableName, id).Quality;
    elseif (type == ItemType.Chest) then    --宝箱
        return DataManager.GetDataEntity(GameLogic.chestDataTableName, id).Quality;
    elseif (type == ItemType.Gold) then     --金币
        return 3;
    elseif (type == ItemType.Diamond) then  --元宝
        return 4;
    elseif (type == ItemType.Exp) then      --经验
        return 1;
    end
end

function GameLogic.GetItemCount(type, id)
    if (type == ItemType.Resource) then      --物资
        return ResManager.GetResourceCount(tonumber(id));
    elseif (type == ItemType.Hero) then      --武将
        return GameLogic.GetHeroCount(tonumber(id));
    elseif (type == ItemType.Prop) then      --道具
        return PropManager.GetPropCount(tonumber(id));
    elseif (type == ItemType.Chest) then     --宝箱
        return ChestManager.GetChestCount(tonumber(id));
    end
end

function GameLogic.CurrentServerTimeMS()
    return this.startServerTime + tonumber(tostring(System.DateTime.Now.Ticks / 10000 - this.startLocalTime));
end

function GameLogic.CurrentServerTimeSecond()
    return (this.startServerTime + tonumber(tostring(System.DateTime.Now.Ticks / 10000 - this.startLocalTime))) / 1000;
end