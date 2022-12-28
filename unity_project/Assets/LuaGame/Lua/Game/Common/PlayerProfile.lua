PlayerProfile = class("PlayerProfile");

local this = PlayerProfile;
this.KeyBGMMute = "BGMMute";
this.KeyAudioMute = "AudioMute";

function PlayerProfile.SaveBGMMute(mute)
    PlayerProfileBase.SaveBoolValue(this.KeyBGMMute, mute);
end

function PlayerProfile.LoadBGMMute()
    return PlayerProfileBase.LoadBoolValue(this.KeyBGMMute, false);
end

function PlayerProfile.SaveAudioMute(mute)
    PlayerProfileBase.SaveBoolValue(this.KeyAudioMute, mute);
end

function PlayerProfile.LoadAudioMute()
    return PlayerProfileBase.LoadBoolValue(this.KeyAudioMute, false);
end
