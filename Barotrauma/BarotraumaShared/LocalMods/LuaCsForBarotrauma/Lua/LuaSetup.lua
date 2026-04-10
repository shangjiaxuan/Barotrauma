LuaSetup = {}

local path = ...

local function AddTableToGlobal(tbl)
    for k, v in pairs(tbl) do
        _G[k] = v
    end
end

if SERVER then
    AddTableToGlobal(dofile(path .. "/Lua/DefaultLib/LibServer.lua"))
else
    AddTableToGlobal(dofile(path .. "/Lua/DefaultLib/LibClient.lua"))
end

AddTableToGlobal(dofile(path .. "/Lua/DefaultLib/LibShared.lua"))

AddTableToGlobal(dofile(path .. "/Lua/CompatibilityLib.lua"))

dofile(path .. "/Lua/DefaultHook.lua")

Descriptors = LuaUserData

dofile(path .. "/Lua/DefaultLib/Utils/Math.lua")
dofile(path .. "/Lua/DefaultLib/Utils/String.lua")
dofile(path .. "/Lua/DefaultLib/Utils/Util.lua")
dofile(path .. "/Lua/DefaultLib/Utils/SteamApi.lua")

if not CSActive then
    for k, v in pairs(debug) do
        if k ~= "getmetatable" and k ~= "setmetatable" and k ~= "traceback" then
            debug[k] = nil
        end
    end
end

if SERVER then
    Networking.Receive("_luastart", function (message, client)
        local num = message.ReadUInt16()

        local packages = {}

        for i = 1, num, 1 do
            table.insert(packages, {
                Name = message.ReadString(),
                Version = message.ReadString(),
                Id = message.ReadUInt64(),
                Hash = message.ReadString()
            })
        end

        Hook.Call("client.packages", client, packages)
    end)
elseif Game.IsMultiplayer then
    local message = Networking.Start("_luastart")

    local packageCount = 0
    for package in ContentPackageManager.EnabledPackages.All do packageCount = packageCount + 1 end

    message.WriteUInt16(packageCount)

    for package in ContentPackageManager.EnabledPackages.All do
        local id = package.UgcId
        local hash = package.Hash and package.Hash.StringRepresentation or ""

        if id == nil then id = 0 end

        message.WriteString(package.Name)
        message.WriteString(package.ModVersion)
        message.WriteUInt64(UInt64(id))
        message.WriteString(hash)
    end

    Networking.Send(message)
end

LuaSetup = nil