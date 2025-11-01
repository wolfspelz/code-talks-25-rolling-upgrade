using My.Interfaces;

namespace My.Logic;

public class Grid : Grain, IGrid
{
    public Grid(
    )
    {
    }

    public async Task<FieldProperties[][]> Get(int nx, int ny) {
        var id = this.GetPrimaryKeyString();

        FieldProperties[][] result = new FieldProperties[nx][];
        for (int ix = 0; ix < nx; ix++) {
            result[ix] = new FieldProperties[ny];
            for (int iy = 0; iy < ny; iy++) {
                var grain = GrainFactory.GetGrain<IField>($"{id}-{ix}-{iy}");
                result[ix][iy] = await grain.Get();
            }
        }
        return result;
    }
}
