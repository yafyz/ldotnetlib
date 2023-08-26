local MathF = t'System.MathF';

function NewParticleManager()
    local pm = {
        width = 0;
        height = 0;

        gravity = 1;
        gravity_max = 20;

        ---@class Particle
        ---@field x number
        ---@field y number
        ---@field szx number
        ---@field szy number
        ---@field vx number
        ---@field vy number
        ---@field brush any

        ---@type Particle[]
        particles = {}
    }

    local particles = pm.particles;
    local rnd = t'System.Random'.Shared;

    function pm:Explosion(x, y, szx, szy, intensity, count, brush)
        local sz = #particles;
        for i=1, count do
            local rad = tolua(rnd.NextDouble())*tolua(MathF.PI)*2;
            particles[sz+i] = {
                x = x;
                y = y;

                szx = szx;
                szy = szy;

                vx = tolua(MathF.Cos(rad))*tolua(rnd.NextDouble())*intensity;
                vy = tolua(MathF.Sin(rad))*tolua(rnd.NextDouble())*intensity;

                brush = brush;
            }
        end
    end

    function pm:Tick()
        local gravity = self.gravity;
        local term = self.gravity_max;
        local width = pm.width;
        local height = pm.height;

        for i,v in next, particles do
            if v.vy > term*-1 then
                v.vy = v.vy - gravity;
            end

            v.x = v.x + v.vx;
            v.y = v.y - v.vy;

            if v.x + v.szx/2 < 0 or v.x - v.szx > width or v.y-v.szy/2 > height then
                particles[i] = nil;
            end
        end
    end

    function pm:Draw(g)
        for _,v in next, particles do
            g.FillRectangle(v.brush, v.x-v.szx/2, v.y-v.szy/2, v.szx, v.szy);
        end
    end

    return pm;
end