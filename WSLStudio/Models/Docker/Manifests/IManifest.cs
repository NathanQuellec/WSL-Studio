namespace WSLStudio.Models.Docker.Manifests;

public interface IImageManifest
{
    public List<string> getLayers();
}