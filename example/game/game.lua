local Color = t'System.Drawing.Color'
local Pen = t'System.Drawing.Pen'
local RectangleF = t'System.Drawing.RectangleF'
local MathF = t'System.MathF'
local StringAlignment = t'System.Drawing.StringAlignment'
local Font = t'System.Drawing.Font'
local Brushes = t'System.Drawing.Brushes'
local GraphicsPath = t'System.Drawing.Drawing2D.GraphicsPath'

local BRICK_WIDTH = 50;
local BRICK_HEIGHT = 25;

local BRICK_SCORES = {
    ["red"] = 1;
    ["blue"] = 3;
    ["yellow"] = 10;
    ["black"] = 0;
}

local BRICK_COLORS = {
    ["red"] = {255, 0, 0};
    ["blue"] = {0, 0, 255};
    ["yellow"] = {255, 255, 0};
    ["black"] = {0, 0, 0};
}

local BRICK_BLUE_COUNT = 3;
local BRICK_YELLOW_COUNT = 1;
local BRICK_BLACK_COUNT = 1;

local function NewDrawable()
    ---@class Drawable
    local drawable = {
        x = 0;
        y = 0;
        width = 0;
        height = 0;

        rect = new(RectangleF,0,0,0,0);

        ---@type fun(self: Drawable, graphics): nil
        Draw = nil;
    }

    ---@param drw Drawable
    local function RegenerateRect(drw)
        drw.rect = new(RectangleF, drw.x, drw.y, drw.width, drw.height)
    end

    function drawable:SetSize(width, height)
        self.width = width
        self.height = height
        RegenerateRect(self)
    end;

    function drawable:SetPosition(x, y)
        self.x = x
        self.y = y
        RegenerateRect(self)
    end;

    return drawable
end

---@alias BrickType "red" | "blue" | "yellow" | "black"

---@param type BrickType
---@param row integer
---@param col integer
local function NewBrick(type, row, col)
    ---@class Brick : Drawable
    local brick = NewDrawable()
    brick.type = type
    brick.row = row
    brick.col = col

    brick.color = Color.Transparent

    ---@param a integer?
    function brick:SetColor(r,g,b,a)
        self.color = Color.FromArgb(a or 255, r, g, b)
    end

    function brick:Draw(g)
        g.FillRectangle(new(t'System.Drawing.SolidBrush', self.color), self.rect)
        g.DrawRectangle(new(Pen, Color.FromArgb(0, 0, 0), 2), self.x, self.y, self.width, self.height)
    end

    return brick;
end

local function NewScope()
    ---@class Scope : Drawable
    local scope = NewDrawable()
    scope.pointX = 100;
    scope.pointY = 50;

    scope.width = 20;
    scope.height = 20;

    scope.speed = 2;

    ---@return number x, number y
    function scope:GetMiddle()
        return self.x+self.width/2, self.y+self.height/2
    end

    function scope:Draw(g)
        local p = new(Pen, Color.FromArgb(100, 100, 100), 2)
        g.DrawLine(p, self.x, self.y, self.x+self.width, self.y+self.height)
        g.DrawLine(p, self.x, self.y+self.height, self.x+self.width, self.y)
    end

    function scope:Tick(size)
        local adjacent = self.pointX-self.x
        local opposite = self.pointY-self.y
        local hyp = tolua(MathF.Sqrt(adjacent*adjacent + opposite*opposite))

        local y_dir = opposite/hyp
        local x_dir = adjacent/hyp

        self.x = self.x + self.speed*x_dir
        self.y = self.y + self.speed*y_dir

        if hyp < self.speed then
            local rnd = t'System.Random'.Shared
            self.pointX = tolua(rnd.NextDouble())*(tolua(size.Width)-self.width)
            self.pointY = tolua(rnd.NextDouble())*(tolua(size.Height)-self.height)
        end
    end

    return scope;
end

function NewGame()
    ---@class Game
    local game = {
        form = new(t'System.Windows.Forms.Form');
        container = new(t'System.Windows.Forms.PictureBox');
        timer = new(t'System.Windows.Forms.Timer');
        scope = NewScope();

        shaker = NewShaker();
        pm = NewParticleManager();
        styleboard = nil;

        ---@type Brick[][]
        bricks = {};
        score = 0;

        shots_remaining = 3;
        game_over = false;
        cleaned_up = false;
    }

    game.shaker.form = game.form;
    game.pm.height = tolua(game.form.Height);
    game.pm.width = tolua(game.form.Width);
    game.styleboard = NewStyleBoard(game.form);

    game.form.FormBorderStyle = t'System.Windows.Forms.FormBorderStyle'.FixedDialog;
    game.form.MaximizeBox = false;

    game.container.Size = game.form.ClientSize
    game.form.Controls.Add(game.container)

    game.timer.Interval = 1/30*1000;
    game.timer.Enabled = true;

    local x_off = tolua(game.container.Width)/2-BRICK_WIDTH*4/2
    local y_off = tolua(game.container.Height)/2-BRICK_HEIGHT*5/2

    ---@class brick_t
    ---@field row integer
    ---@field col integer
    ---@field type BrickType

    ---@type brick_t[]
    local brick_types = {}

    local rnd = t'System.Random'.Shared;

    local function get_brick_type(row, col)
        for _, d in next, brick_types do
            if d.row == row and d.col == col then
                return d.type
            end
        end
    end

    local function get_row_len(row)
        return not (row % 2 == 0) and 4 or 3
    end

    ---@param type BrickType
    ---@param count integer
    ---@param extra_check? fun(row: integer, col: integer): boolean
    local function place_bricks(type, count, extra_check)
        for _=1, count do
            local row ---@type integer
            local col ---@type integer

            repeat
                row = tolua(rnd.Next(5))+1
                col = tolua(rnd.Next(get_row_len(row)))+1
            until get_brick_type(row, col) == nil
                   and (not extra_check and true or extra_check(row, col))

            brick_types[#brick_types+1] = {row=row, col=col, type=type}
            print("brick", row, col, type)
        end
    end

    place_bricks("blue", BRICK_BLUE_COUNT)
    place_bricks("yellow", BRICK_YELLOW_COUNT)
    place_bricks("black", BRICK_BLACK_COUNT, function(row, col)
        return not (row == 1 or row == 5 or col == 1 or col == get_row_len(row))
    end)

    for row=1, 5 do
        game.bricks[row] = {}
        local even = row % 2 == 0

        for col=1, not even and 4 or 3 do
            local type = get_brick_type(row, col) or "red"
            local brick = NewBrick(type, row, col)

            if type ~= "red" then
                print(type, brick, row*col, row, col)
            end

            brick:SetColor(unpack(BRICK_COLORS[type]))
            brick:SetSize(BRICK_WIDTH, BRICK_HEIGHT)
            brick:SetPosition((col-1)*BRICK_WIDTH + (even and BRICK_WIDTH/2 or 0) + x_off, (row-1)*BRICK_HEIGHT+y_off)

            game.bricks[row][col] = brick;
        end
    end

    local function get_brick_at_pos(x,y)
        for _,row in next, game.bricks do
            for _,brick in next, row do
                if brick.rect.Contains(x,y) then
                    return brick;
                end
            end
        end
    end

    ---@param brick Brick
    local function destroy_brick(brick)
        if not brick then
            print("attempt to destroy nil brick")
            return
        end
        game.bricks[brick.row][brick.col] = nil;
        game.score = game.score+BRICK_SCORES[brick.type]*game.styleboard.style_multiplier;

        game.shaker:AddShake(10)
        game.pm:Explosion(brick.x+brick.width/2, brick.y+brick.height/2, 10, 10, 10, 20, new(t'System.Drawing.SolidBrush', brick.color))
        game.styleboard:AddStyle(BRICK_SCORES[brick.type], "+"..brick.type)

        local function d(off)
            destroy_brick(game.bricks[brick.row-1][brick.col+off])
            destroy_brick(game.bricks[brick.row-1][brick.col])

            destroy_brick(game.bricks[brick.row][brick.col-1])
            destroy_brick(game.bricks[brick.row][brick.col+1])

            destroy_brick(game.bricks[brick.row+1][brick.col+off])
            destroy_brick(game.bricks[brick.row+1][brick.col])
        end

        if brick.type == "black" then
            if brick.row % 2 ~= 0 then
                d(-1)
            else
                d(1)
            end
        end
    end

    local on_key_down = method(function(_, e)
        if game.game_over then
            game.form.Close()
            return;
        end

        local keycode = tolua(e.KeyCode);

        if keycode == 32 then
            -- 32 = space
            local px, py = game.scope:GetMiddle()
            local brick = get_brick_at_pos(px,py)

            print(px,py, brick)
            game.shots_remaining = game.shots_remaining - 1;
            if brick then
                destroy_brick(game.bricks[brick.row][brick.col])
            end
            if game.shots_remaining < 1 then
                game.game_over = true;
            end
        elseif keycode == 65 then
            -- 65 = A
            local brick
            for _,row in next, game.bricks do
                for _,br in next, row do
                    if br and br.type == "black" then
                        brick = br
                    end
                end
            end
            if brick then
                game.scope.pointX = brick.x + 10;
                game.scope.pointY = brick.y + 10;
            end
        end
    end)

    event.add(game.form.KeyDown, on_key_down)

    local arial_10 = new(Font, "Arial", 10)
    local arial_20 = new(Font, "Arial", 20)
    local string_format = new(t'System.Drawing.StringFormat');
    event.add(game.container.Paint, method(function(_, e)
        local g = e.Graphics;
        for _,row in next, game.bricks do
            for _,brick in next, row do
                if brick then
                    brick:Draw(g)
                end
            end
        end

        game.pm:Draw(g)
        game.scope:Draw(g)
        game.styleboard:Draw(g, game.form.ClientSize.Width, game.form.ClientSize.Height)

        if not game.game_over then
            g.DrawString("Score: "..game.score.."\nShots: "..game.shots_remaining, arial_10, Brushes.Black, 0, 0)
        else
            if game.styleboard.current_style then
                string_format.Alignment = StringAlignment.Near;
            else
                string_format.Alignment = StringAlignment.Center;
            end
            string_format.LineAlignment = StringAlignment.Near;
            g.DrawString("Score: "..game.score, arial_20, Brushes.Black, game.container.ClientRectangle, string_format)

            local gp = new(GraphicsPath)
            string_format.Alignment = StringAlignment.Center;
            string_format.LineAlignment = StringAlignment.Center;
            gp.AddString("Game Over", new(t'System.Drawing.FontFamily', "Arial"), t'System.Drawing.FontStyle'.Regular, 80, game.container.ClientRectangle, string_format)
            g.FillPath(Brushes.Black, gp)
            g.DrawPath(new(Pen, Brushes.White, 2), gp);

            string_format.LineAlignment = StringAlignment.Far;
            g.DrawString("Press any key to exit", arial_20, Brushes.Black, game.container.ClientRectangle, string_format)
        end
    end))

    event.add(game.timer.Tick, method(function(_, _)
        game.shaker:Tick()
        game.pm:Tick()
        game.styleboard:Tick()

        if not game.game_over then
            game.scope:Tick(game.container.Size)
        else
            if not game.cleaned_up then
                game.cleaned_up = true
            end
        end
        game.container.Refresh()
    end))

    return game;
end