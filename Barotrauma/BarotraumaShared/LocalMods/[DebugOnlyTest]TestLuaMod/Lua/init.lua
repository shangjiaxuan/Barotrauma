print("Hello!")

Hook.Add("character.created", "test", function(character) 
    print("character.created: ", character)
end)

Hook.Add("character.death", "test", function(character) 
    print("character.death: ", character)
end)

Hook.Add("character.giveJobItems", "test", function(character) 
    print("character.giveJobItems: ", character)
end)

Hook.Add("roundStart", "test", function()
    print("roundStart")
end)

Hook.Add("roundEnd", "test", function()
    print("roundEnd")
end)

Hook.Add("missionsEnded", "test", function()
    print("missionsEnded")
end)

local failed, package = trygetpackage("[DebugOnlyTest]TestLuaMod")

print("packageFailed=", failed)
print("package", package.Name)

local success, config = ConfigService.TryGetConfig(SettingBase.Single, package, "TestFloat")

local success2, config2 = ConfigService.TryGetConfig(SettingBase.Int32, package, "TestSynchroServer")
local success3, config3 = ConfigService.TryGetConfig(SettingBase.Int32, package, "TestSynchroClient")

print("config ", success, " ", config.Value)
print("config testsynchrosrv", success2, " ", config2.Value)
print("config testsynchrocli", success3, " ", config3.Value)

local lastTime = 0

Hook.Add("think", "printconfig", function()
    if lastTime > Timer.Time then return end

    lastTime = Timer.Time + 10
    print(config.Value)

    if SERVER then
        config.TrySetValue(config.Value + 1)
    end
end)
