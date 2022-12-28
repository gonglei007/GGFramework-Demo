local machine = require("3rd.lua-state-machine.statemachine");
require("Managers.NetworkManager");
require("Card.CardStateMachine.CardStateMachineManager")
require("Card.Ctrl.BattleSceneCtrl")
require("Card.Ctrl.StartMatchCtrl")
require("Card.Ctrl.StartMatchCtrl")
---
--- 房间类。管理着房间系统的整体状态。
---
Room = class("Room")
local this = Room;
local instance = null;

function Room:ctor()
    local fsm = machine.create({
        initial = 'idle',
        events = {
            { name = 'init',  from = 'idle',  to = 'initial' },
            { name = 'connect',  from = 'initial',  to = 'connection' },
            { name = 'load',  from = 'connection',  to = 'loading' },
            { name = 'build',  from = 'loading',  to = 'fetching' },
            --{ name = 'create',  from = 'building',  to = 'fetching' },
            { name = 'enter',  from = 'fetching',  to = 'game' },
            { name = 'exit', from = 'game', to = 'idle' },
        },
        callbacks = {
            oninit = Room.OnInit,
            onconnect = Room.OnConnect,
            onload = Room.OnLoad,
            onbuild = Room.OnBuild,
            --
            onenter =  Room.OnEnter,
            onexit =  Room.OnExit,
        }
    });
    self.fsm = fsm;
    self.roomMax = 0;
    instance = self;
end

function Room.Instance()
    if instance == nil then
        instance = Room.new();
    end
    return instance;
end

function Room:GetFSM()
    return self.fsm;
end

function Room:GetRoomMax()
    return self.roomMax;
end

function Room:SetRoomMax(max)
    self.roomMax = max;
end


---
--- 重置房间数据。
---
function Room:Reset()
    --print("GL2-->Room:Reset");
end

local stateSwitch = {
    ["loading"] = function()
        if resourceSystem:GetLoadingState(ResourceSystem.Group.Room) == ResourceSystem.State.Loaded then
            instance:GetFSM():build();
        end
    end,
}
function Room:Update(deltaTime)
    SelectUICtrl.Instance():Update();
    CardBattleUICtrl.Instance():Update(deltaTime);
    CardStateMachineManager.update(deltaTime);
    local state = self.fsm.current;
    --print('@@@@@@->'..state);
    local fun = stateSwitch[tostring(state)];
    if(fun) then
        fun();
    else

    end
end

---
--- 初始化房间
---
function Room.OnInit()
    print("Ready to..."..instance:GetFSM().current);
    instance:Reset();
    instance:GetFSM():connect();
end

function Room.OnConnect(self, event, from, to, msg)
    local currentState = instance:GetFSM().current;
    print("Ready to..."..currentState);
    math.randomseed(tostring(os.time()):reverse():sub(1, 7))
    NetworkManager.Init(HttpNetworkManager.gameServerHost, HttpNetworkManager.gameServerPost, "go-client", function()
        print("连接成功..."..currentState);
        NetworkManager.EnterGame(function (server_data)
            print("EnterGame_______"..server_data)
            local data = cjson.decode(server_data);
            local code = tonumber(data["code"]);
            if (200 == code) then
                local player = data["player"];
                instance.roomMax = tonumber(data["max"]);
                Player.uid = player["uid"];
                Player.nick = player["nick"];
                Player.winNum = player["winNum"];
                -- MatchCtrl.Instance():Present();
                Player.rewardState = data["rewardState"];
                Player.showCard5 = tonumber(data["showCardSet5"]);
                Player.room = data["room"];
                -- 只有从连接状态请求，才会执行load方法。
                --print("[RECONN]>>>>>>>>>>>>>>"..tostring(currentState));
                if(tostring(currentState) == "connection") then
                    instance:GetFSM():load();
                end
                CardBattleUIView.Instance():CloseSelectHeroPage();
            elseif code == 300 then---重连失败
                if BattleSceneView.Instance().gameObject ~= nil and BattleSceneView.Instance().gameObject.activeInHierarchy then
                    uiSystem:DismissAllPage();
                    uiSystem:ShowLoading("UILoading");
                    LobbyModel.homeForLobby = 0;
                    Game.RoomExit();
                    CardStateMachineManager.Instance():Clear();
                    stageSystem.Instance:ChangeStage(StageSystem.Lobby);
                else
                    stageSystem.Instance:ChangeStage(StageSystem.Lobby);
                end
            end
        end)
    end);
end

function Room.OnLoad()
    --ResourceManager.LoadRoom();
    instance:GetFSM():build();
end

function Room.OnBuild()
    UnityEngine.Resources.UnloadUnusedAssets();
    -- ArrowManager.Instance():CreateArrows(10);
    -- VFXManager.Instance():Init();
    -- BattleUICtrl.Instance():InitResources();
    -- BattleSceneCtrl.Instance():InitResources();
    instance:GetFSM():enter();
end

function Room.OnEnter(self, event, from, to, msg)
    --uiSystem:HideLoading("UISwitch_in");
    if Player.room ~= nil then
        SelectUICtrl.SetSelfInfo(Player.nick,Player.uid,Player.winNum,Player.rewardState,Player.room,Player.showCard5);
    else
        SelectUICtrl.SetSelfInfo(Player.nick,Player.uid,Player.winNum,Player.rewardState,nil,Player.showCard5);
    end
    Room.OnStartGame();
end

function Room.OnStartGame()
    -- local CardStateUpObj = GameObject.New("CardStateUp");
    -- CardStateUpObj:AddComponent(typeof(CardStateSetUp));
    CardStateMachineManager.awake();
    print("Start Lua Card State");
end

function Room.OnExit(self, event, from, to, msg)
    print("Ready to...." ..instance:GetFSM().current);
end
