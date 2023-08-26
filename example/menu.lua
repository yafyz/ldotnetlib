local Point = t'System.Drawing.Point'
local Size = t'System.Drawing.Size'
local Font = t'System.Drawing.Font'

local function center_control(con, form)
    con.Location = new(Point, tolua(form.Width)/2-tolua(con.Width)/2, con.Location.Y)
end

local function after_control(con, before, extra_spacing)
    con.Location = new(Point, con.Location.X, tolua(before.Location.Y)+tolua(before.Height)+(extra_spacing or 0))
end

local function AskForName()
    local dialog = {
        form = new(t'System.Windows.Forms.Form');
        lh1 = new(t'System.Windows.Forms.Label');
        textbox = new(t'System.Windows.Forms.TextBox');

        submit_btn = new(t'System.Windows.Forms.Button');
        cancel_btn = new(t'System.Windows.Forms.Button');

        name = "";
    }

    dialog.form.FormBorderStyle = t'System.Windows.Forms.FormBorderStyle'.FixedDialog;

    dialog.lh1.Text = "Zadejte jm\195\169no pro ulo\197\190en\195\173 sk\195\179re";
    dialog.lh1.Size = new(Size, dialog.form.ClientSize.Width, dialog.lh1.Size.Height)
    dialog.lh1.TextAlign = t'System.Drawing.ContentAlignment'.MiddleCenter

    after_control(dialog.textbox, dialog.lh1, 5);
    center_control(dialog.textbox, dialog.form);

    dialog.submit_btn.Text = "ulozit";
    after_control(dialog.submit_btn,dialog.textbox, 5);
    dialog.submit_btn.Location = new(Point, tolua(dialog.form.Width)/2-tolua(dialog.submit_btn.Size.Width)-2, dialog.submit_btn.Location.Y)

    dialog.cancel_btn.Text = "neukladat";
    dialog.cancel_btn.Location = new(Point, tolua(dialog.form.Width)/2+2, dialog.submit_btn.Location.Y)

    dialog.form.Height = tolua(dialog.cancel_btn.Location.Y) + tolua(dialog.cancel_btn.Height)
                           + (tolua(dialog.form.Height)-tolua(dialog.form.ClientSize.Height)) + 5

    dialog.form.Controls.Add(dialog.lh1);
    dialog.form.Controls.Add(dialog.textbox);
    dialog.form.Controls.Add(dialog.submit_btn);
    dialog.form.Controls.Add(dialog.cancel_btn);

    event.add(dialog.submit_btn.Click, method(function(_,_)
        dialog.name = tolua(dialog.textbox.Text.Trim())
        dialog.form.Close()
    end))

    event.add(dialog.cancel_btn.Click, method(function(_,_)
        dialog.form.Close()
    end))

    return dialog;
end

function NewMenu()
    ---@class Menu
    local menu = {
        form = new(t'System.Windows.Forms.Form');
        lh1 = new(t'System.Windows.Forms.Label');
        lh2 = new(t'System.Windows.Forms.Label');
        play_btn = new(t'System.Windows.Forms.Button');
        leaderboard_btn = new(t'System.Windows.Forms.Button');
        exit_btn = new(t'System.Windows.Forms.Button');
    }

    menu.form.FormBorderStyle = t'System.Windows.Forms.FormBorderStyle'.FixedSingle;
    menu.form.MaximizeBox = false;

    menu.lh1.Text = "cihlo hra"
    menu.lh1.Location = new(Point, 0, 5);
    menu.lh1.Font = new(Font, "Arial", 20)
    menu.lh1.Size = new(Size, menu.form.ClientSize.Width, menu.lh1.Size.Height)
    menu.lh1.TextAlign = t'System.Drawing.ContentAlignment'.MiddleCenter

    menu.lh2.Text = "jak vid\196\155no na *nespecifikovan\195\169m zad\195\161n\195\173*"
    menu.lh2.Size = new(Size, menu.form.ClientSize.Width, menu.lh2.Size.Height)
    menu.lh2.TextAlign = t'System.Drawing.ContentAlignment'.MiddleCenter
    after_control(menu.lh2, menu.lh1);

    menu.play_btn.Text = "hraj";
    menu.play_btn.Location = new(Point, 0, 100)
    center_control(menu.play_btn, menu.form)

    menu.leaderboard_btn.Text = "zebricek";
    after_control(menu.leaderboard_btn, menu.play_btn, 5)
    center_control(menu.leaderboard_btn, menu.form)

    menu.exit_btn.Text = "ukoncit";
    after_control(menu.exit_btn, menu.leaderboard_btn, 50)
    center_control(menu.exit_btn, menu.form)

    menu.form.Controls.Add(menu.lh1);
    menu.form.Controls.Add(menu.lh2);
    menu.form.Controls.Add(menu.play_btn);
    menu.form.Controls.Add(menu.leaderboard_btn);
    menu.form.Controls.Add(menu.exit_btn);

    event.add(menu.play_btn.Click, method(function(_,_)
        local game = NewGame()
        menu.form.Hide()
        game.form.StartPosition = t'System.Windows.Forms.FormStartPosition'.Manual;
        game.form.Location = menu.form.Location;

        game.form.ShowDialog()
        menu.form.Location = game.form.Location;

        if game.game_over then
            local dialog = AskForName()
            dialog.form.StartPosition = t'System.Windows.Forms.FormStartPosition'.Manual;
            dialog.form.Location = menu.form.Location;
            dialog.form.ShowDialog();
            menu.form.Location = dialog.form.Location;

            if dialog.name ~= "" then
                AppendScore(tolua(t'System.DateTime'.Now.ToString()), dialog.name, game.score);
            end
        end

        menu.form.Show()
    end))

    event.add(menu.leaderboard_btn.Click, method(function(_,_)
        local lb = NewLeaderboard()
        menu.form.Hide()
        lb.form.StartPosition = t'System.Windows.Forms.FormStartPosition'.Manual;
        lb.form.Location = menu.form.Location;
        lb.form.ShowDialog()
        menu.form.Location = lb.form.Location;
        menu.form.Show()
    end))

    event.add(menu.exit_btn.Click, method(function (_,_)
        menu.form.Close()
    end))

    return menu;
end