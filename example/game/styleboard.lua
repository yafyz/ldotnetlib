local Rectangle = t'System.Drawing.Rectangle';
local Color = t'System.Drawing.Color';
local StringAlignment = t'System.Drawing.StringAlignment'
local GraphicsPath = t'System.Drawing.Drawing2D.GraphicsPath'
local Brushes = t'System.Drawing.Brushes'
local Point = t'System.Drawing.Point';
local Font = t'System.Drawing.Font';
local SystemInfo = t'System.Windows.Forms.SystemInformation';

local WIDTH_OFFSET = 300;
local STYLE_BAR_OFFSET = 30;
local STYLE_SUBSTYLE_OFFSET = 40;

local OVERFLOW_INSET = 5;

local STYLES = {
    [0] = "DESTRUCTIVE";
    [2] = "CHAOTIC";
    [3] = "BRUTAL";
    [4] = "ANARCHIC";
    [5] = "SUPREME";
    [6] = "SSADISTIC";
    [7] = "SSSHITSTORM";
    [8] = "ULTRABRICK";
}

local STYLE_MULTIPLIER = 20;

function NewStyleBoard(form)
    local board = {
        ---@type string?
        current_style = nil;

        ---@class Substyle
        ---@field str string
        ---@field tleft integer

        ---@type Substyle[]
        substyles = {};

        style = 0;
        curr_min = 0;
        curr_max = 0;
        style_multiplier = 1;

        form = form;
        overflow_form = new(t'System.Windows.Forms.Form');
    }

    local overflow_shown = true;
    local function align_location()
        local f = board.overflow_form;
        local y_off = tolua(board.form.Size.Height)-tolua(board.form.ClientSize.Height)-tolua(SystemInfo.Border3DSize.Height)-1;
        local x_off = tolua(SystemInfo.Border3DSize.Width)+1;

        f.Location = new(Point,
            tolua(board.form.Location.X)-tolua(f.Width)+x_off+OVERFLOW_INSET,
            tolua(board.form.Location.Y)+y_off+STYLE_BAR_OFFSET
        );
    end

    board.overflow_form.FormBorderStyle = t'System.Windows.Forms.FormBorderStyle'.None;
    board.overflow_form.Show();
    board.overflow_form.ShowInTaskbar = false;
    board.overflow_form.Height = 10;
    board.overflow_form.Width = 0;
    board.overflow_form.BackColor = Color.Black;

    event.add(board.form.Closing, method(function(_,_)
        print("closing style meter overflow")
        board.overflow_form.Close()
    end))

    event.add(board.form.LocationChanged, method(function(_,_)
        align_location()
    end))

    function board:AddStyle(style, substyle)
        self.style = self.style+style*STYLE_MULTIPLIER;
        board.substyles[#board.substyles+1] = {str = substyle; tleft = 120}
    end

    function board:Tick()
        if self.style > 0 then
            self.style = self.style-1;
        end
        for i,v in next, board.substyles do
            v.tleft = v.tleft-1;
            if v.tleft < 1 then
                board.substyles[i] = nil;
            end
        end

        self.current_style = nil;
        for i,v in next, STYLES do
            if i*STYLE_MULTIPLIER < self.style then
                self.current_style = v;
                self.curr_min = i*STYLE_MULTIPLIER;
                self.curr_max = (i+1)*STYLE_MULTIPLIER
                self.style_multiplier = 1+(i+1)/4;
            else
                break;
            end
        end
    end

    function board:Draw(g, width, height)
        width = tolua(width)
        local rect = new(Rectangle, width-WIDTH_OFFSET, 10, WIDTH_OFFSET, 20);
        local string_format = new(t'System.Drawing.StringFormat');
        string_format.Alignment = StringAlignment.Far;
        string_format.LineAlignment = StringAlignment.Center;

        local arial_20 = new(Font, "Arial", 15)
        local arial_10 = new(Font, "Arial", 10)

        g.DrawString(board.current_style or "", arial_20, Brushes.Black, rect, string_format)

        local substylestr = "";
        for _, v in next, board.substyles do
            local a = substylestr..v.str.."\n";
            substylestr = a;
        end

        if self.style > 0 then
            local bar_len = 90 * (self.style-self.curr_min)/(self.curr_max-self.curr_min);
            local overflow = bar_len-width-5;
            g.FillRectangle(Brushes.Black, width-bar_len-5, STYLE_BAR_OFFSET, bar_len, 10);
            if overflow >= 0 then
                board.overflow_form.Width = overflow+OVERFLOW_INSET;
                align_location();
                overflow_shown = true;
            elseif overflow_shown then
                board.overflow_form.Width = 0;
                overflow_shown = false;
                align_location();
            end
        end

        rect.Y = STYLE_SUBSTYLE_OFFSET;
        rect.Height = height;
        string_format.LineAlignment = StringAlignment.Near;
        g.DrawString(substylestr, arial_10, Brushes.Black, rect, string_format)
    end

    align_location()

    return board;
end