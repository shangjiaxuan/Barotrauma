# How to use hooks

Hooks are basically functions that get called when events happen in-game, like chat messages. They can be triggered either by Lua itself or the game code.

## Adding hooks

Hooks can be added like this:

```
Hook.Add("chatMessage", "test", function(message, client)
    print(client.Name .. " has sent " .. message)
end)
```

The event name (first argument), is case-insensitive, so you can call it chatMessage, cHaTmEsSaGe, chatmessage, etc.

## Calling hooks

You can also call hooks with the following code:

```
Hook.Call("myCustomEvent", {"some", "arguments", 123})
```

## XML Status Effect Hooks

With Lua, a new XML tags is added, it can be used to call hooks inside status effects:

```
<StatusEffect type="OnUse">
    <Hook name="doSomething" custom="thing" />
</StatusEffect>
```

```
Hook.Add("doSomething", function(effect, deltaTime, item, targets, worldPosition, element)
    print(effect, ' ', item)

    -- You can also access the XML custom parameters
    print(element.GetAttributeString("custom", "default value"))
end)
```

## Patching

Patching allows you to hook into existing methods in the game code.
Be aware that it can be a little unstable depending on the method that you are patching.

If your lua function returns **any** value (including `nil`), it will replace
the return value of the original method.

### Postfix

Postfixes are functions that get called after the original method executes.

```
Hook.Patch("Barotrauma.CharacterInfo", "IncreaseSkillLevel", function(instance, ptable)
  print(string.format("%s gained % xp", instance.Character.Name, ptable["increase"]))
end, Hook.HookMethodType.After)
```

### Prefix

Prefixes are functions that get called before the original method executes.
For more advanced use cases, they can also be used to modify the incoming
parameters or prevent the original method from executing.

```
Hook.Patch(
  "Barotrauma.Character",
  "CanInteractWith",
  {
    "Barotrauma.Item",
     -- ref/out parameters are supported
     "out System.Single",
     "System.Boolean"
  },
  function(instance, ptable)
    -- This prevents the original method from executing, so we're
    -- effectively replacing the method entirely.
    ptable.PreventExecution = true
    -- Modify the `out System.Single` parameter
    ptable["distanceToItem"] = Single(50)
    -- This changes the return value to "null"
    return nil
  end, Hook.HookMethodType.Before)
```
