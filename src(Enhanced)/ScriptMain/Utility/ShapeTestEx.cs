using System;
using GTA;
using GTA.Math;

namespace TornadoScript.ScriptMain.Utility
{
    public class ShapeTestResult
    {
        public bool DidHit { get; private set; }
        public int HitEntity { get; private set; }
        public Vector3 HitPosition { get; private set; }
        public Vector3 HitNormal { get; private set; }
        public materials HitMaterial { get; private set; }

        public ShapeTestResult(bool didHit, int hitEntity, Vector3 hitPosition, Vector3 hitNormal, materials hitMaterial)
        {
            DidHit = didHit;
            HitEntity = hitEntity;
            HitPosition = hitPosition;
            HitNormal = hitNormal;
            HitMaterial = hitMaterial;
        }
    }

    public static class ShapeTestEx
    {
        public static ShapeTestResult RunShapeTest(Vector3 start, Vector3 end, Entity ignoreEntity, IntersectFlags flags)
        {
            RaycastResult ray = World.Raycast(start, end, flags, ignoreEntity);

            if (!ray.DidHit)
                return new ShapeTestResult(false, 0, Vector3.Zero, Vector3.Zero, materials.none);

            // Normal vector from start to hit point
            Vector3 hitNormal = (ray.HitPosition - start).Normalized;

            // Material hash conversion
            materials material = materials.none;
            try
            {
                material = (materials)ray.MaterialHash;
            }
            catch { }

            int hitEntityHandle = ray.HitEntity?.Handle ?? 0;

            return new ShapeTestResult(true, hitEntityHandle, ray.HitPosition, hitNormal, material);
        }
    }

    public enum materials
    {
        none = -1,
        concrete = 1187676648,
        concrete_pothole = 359120722,
        concrete_dusty = -1084640111,
        tarmac = 282940568,
        tarmac_painted = -1301352528,
        tarmac_pothole = 1886546517,
        rumble_strip = -250168275,
        breeze_block = -954112554,
        rock = -840216541,
        rock_mossy = -124769592,
        stone = 765206029,
        cobblestone = 576169331,
        brick = 1639053622,
        marble = 1945073303,
        paving_slab = 1907048430,
        sandstone_solid = 592446772,
        sandstone_brittle = 1913209870,
        sand_loose = -1595148316,
        sand_compact = 510490462,
        sand_wet = 909950165,
        sand_track = -1907520769,
        sand_underwater = -1136057692,
        sand_dry_deep = 509508168,
        sand_wet_deep = 1288448767,
        ice = -786060715,
        ice_tarmac = -1931024423,
        snow_loose = -1937569590,
        snow_compact = -878560889,
        snow_deep = 1619704960,
        snow_tarmac = 1550304810,
        gravel_small = 951832588,
        gravel_large = 2128369009,
        gravel_deep = -356706482,
        gravel_train_track = 1925605558,
        dirt_track = -1885547121,
        mud_hard = -1942898710,
        mud_pothole = 312396330,
        mud_soft = 1635937914,
        mud_underwater = -273490167,
        mud_deep = 1109728704,
        marsh = 223086562,
        marsh_deep = 1584636462,
        soil = -700658213,
        clay_hard = 1144315879,
        clay_soft = 560985072,
        grass_long = -461750719,
        grass = 1333033863,
        grass_short = -1286696947,
        hay = -1833527165,
        bushes = 581794674,
        twigs = -913351839,
        leaves = -2041329971,
        woodchips = -309121453,
        tree_bark = -1915425863,
        metal_solid_small = -1447280105,
        metal_solid_medium = -365631240,
        metal_solid_large = 752131025,
        metal_hollow_small = 15972667,
        metal_hollow_medium = 1849540536,
        metal_hollow_large = -583213831,
        metal_chainlink_small = 762193613,
        metal_chainlink_large = 125958708,
        metal_corrugated_iron = 834144982,
        metal_grille = -426118011,
        metal_railing = 2100727187,
        metal_duct = 1761524221,
        metal_garage_door = -231260695,
        metal_manhole = -754997699,
        wood_solid_small = -399872228,
        wood_solid_medium = 555004797,
        wood_solid_large = 815762359,
        wood_solid_polished = 126470059,
        wood_floor_dusty = -749452322,
        wood_hollow_small = 1993976879,
        wood_hollow_medium = -365476163,
        wood_hollow_large = -925419289,
        wood_chipboard = 1176309403,
        wood_old_creaky = 722686013,
        wood_high_density = -1742843392,
        wood_lattice = 2011204130,
        ceramic = -1186320715,
        roof_tile = 1755188853,
        roof_felt = -1417164731,
        fibreglass = 1354180827,
        tarpaulin = -642658848,
        plastic = -2073312001,
        plastic_hollow = 627123000,
        plastic_high_density = -1625995479,
        plastic_clear = -1859721013,
        plastic_hollow_clear = 772722531,
        plastic_high_density_clear = -1338473170,
        rubber = -145735917,
        linoleum = 289630530,
        laminate = 1845676458,
        carpet_solid = 669292054,
        cloth = 122789469,
        plaster_solid = -574122433,
        plaster_brittle = -251888898,
        cardboard_sheet = 236511221,
        cardboard_box = -1409054440,
        paper = 474149820,
        foam = 808719444,
        leather = -570470900,
        tvscreen = 1429989756,
        glass_shoot_through = 937503243,
        glass_bulletproof = 244521486,
        glass_opaque = 1500272081,
        water = 435688960,
        blood = 5236042,
        oil = -634481305,
        petrol = -1634184340,
        car_metal = -93061983,
        car_plastic = 2137197282
    }
}
