local Application = t'System.Windows.Forms.Application'
local Environment = t'System.Environment'

local menu = NewMenu()

print("Starting app")
Application.Run(menu.form)
print("Exiting")
Environment.Exit(0)