-- luacheck: ignore 111

--[[--
Class providing timing related things.
]]
-- @code Timer
-- @pragma nostrip

Timer = {}

--- Get time in seconds.
-- @treturn number current time in seconds
-- @realm shared 
function Timer.GetTime() end

--- Calls a function after a certain amount of time.
-- @realm shared 
function Timer.Wait(func, milliseconds) end

--- Calls a function in the next frame
-- @realm shared
function Timer.NextFrame(func) end

--- Same as GetTime()
-- @realm shared 
Timer.Time = 0