-- luacheck: ignore 111

--[[--
Barotrauma Client class with some additional functions and fields

Barotrauma source code: [Client.cs](https://github.com/evilfactory/LuaCsForBarotrauma/blob/master/Barotrauma/BarotraumaShared/SharedSource/Networking/Client.cs)
]]
-- @code Client
-- @pragma nostrip

local Client = {}

-- @remove function SetClientCharacter(character) end
-- @remove function Kick(reason) end
-- @remove function Ban(reason, range, seconds) end
-- @remove function Client.Unban(player, endpoint) end
-- @remove function CheckPermission(permissions) end

--- Sets the client character.
-- @realm server 
function SetClientCharacter(character) end

--- Kick a client.
-- @realm server 
function Kick(reason) end

--- Ban a client.
-- @realm server 
function Ban(reason, range, seconds) end

--- Checks permissions, Client.Permissions.
-- @realm shared 
function CheckPermission(permissions) end

--- Unban a client.
-- @realm server 
function Client.Unban(player, endpoint) end
