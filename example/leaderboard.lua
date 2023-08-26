local File = t'System.IO.File'

local SCORES_FILE = "scores.csv";

function AppendScore(date, name, score)
    File.AppendAllText(SCORES_FILE, date..";"..name..";"..score.."\n")
end

local function is_sorted(scores)
    for i=1, #scores-1 do
        if tonumber(scores[i+1][3]) > tonumber(scores[i][3]) then
            return false;
        end
    end
    return true;
end

---@param scores string[][]
local function sort_scores(scores)
    repeat
        for i=1, #scores-1 do
            local s1 = scores[i];
            local s2 = scores[i+1];

            if tonumber(s2[3]) > tonumber(s1[3]) then
                scores[i] = s2;
                scores[i+1] = s1;
            end
        end
    until is_sorted(scores)
end

---@return string[][]
local function get_scores()
    local scores = {};
    if not File.Exists(SCORES_FILE) then
        return scores
    end

    local text = File.ReadAllText(SCORES_FILE);

    local lines = text.Split("\n", t'System.StringSplitOptions'.None)
    for i=0, tolua(lines.Length)-1 do
        local rows = lines[i].Split(";", t'System.StringSplitOptions'.None)
        if tolua(rows.Length) == 3 then
            scores[#scores+1] = {tolua(rows[0]), tolua(rows[1]), tolua(rows[2])}
        end
    end

    sort_scores(scores);

    return scores;
end

function NewLeaderboard()
    local lb = {
        form = new(t'System.Windows.Forms.Form');
        lv = new(t'System.Windows.Forms.ListView')
    }

    lb.form.FormBorderStyle = t'System.Windows.Forms.FormBorderStyle'.FixedSingle;
    lb.form.MaximizeBox = false;

    lb.lv.Size = lb.form.ClientSize
    lb.lv.View = t'System.Windows.Forms.View'.Details
    lb.form.Controls.Add(lb.lv)

    event.add(lb.form.Load, method(function(_,_)
        for _, v in next, get_scores() do
            local ar = new(t'System.String[]', 3);
            ar[0] = v[1];
            ar[1] = v[2];
            ar[2] = v[3];
            lb.lv.Items.Add(new(t'System.Windows.Forms.ListViewItem', ar));
        end

        lb.lv.Columns.Add("datum", -1);
        lb.lv.Columns.Add("jm\195\169no", 100);
        lb.lv.Columns.Add("sk\195\179re", -2);
    end))
    return lb;
end