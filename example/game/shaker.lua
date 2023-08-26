local Point = t'System.Drawing.Point';
local MathF = t'System.MathF';

function NewShaker()
    local shaker = {
        form = nil;

        posX = 0;
        posY = 0;

        intensity = 0;

        shaking = false;
    }

    function shaker:AddShake(intensity)
        self.intensity = self.intensity + intensity
        self.shaking = true;
        self.posX = tolua(shaker.form.Location.X);
        self.posY = tolua(shaker.form.Location.Y);
    end

    function shaker:Tick()
        if not self.shaking then return end

        local rnd = t'System.Random'.Shared;
        local rad = tolua(rnd.NextDouble())*tolua(MathF.PI)*2;

        local shakeX = tolua(MathF.Cos(rad))*self.intensity;
        local shakeY = tolua(MathF.Sin(rad))*self.intensity;

        shaker.form.Location = new(Point, self.posX+shakeX, self.posY+shakeY);

        if self.intensity > 0 then
            self.intensity = self.intensity - 1;
            self.shaking = true
        elseif self.shaking then
            self.shaking = false;
        end
    end

    return shaker;
end