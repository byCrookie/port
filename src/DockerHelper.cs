namespace dcma;

public static class DockerHelper
{
    public static (string imageName, string tag) GetImageNameAndTag(string imageName)
    {
        var idx = imageName.LastIndexOf(':');
        if (idx == -1)
        {
            throw new ArgumentException("Does not contain tag separator ':'", nameof(imageName));
        }

        return (imageName[..idx], imageName[(idx + 1)..]);
    }

    public static bool TryGetImageNameAndTag(string imageName, out (string imageName, string tag) nameAndTag)
    {
        nameAndTag = (string.Empty, string.Empty);
        var idx = imageName.LastIndexOf(':');
        if (idx == -1)
        {
            return false;
        }

        nameAndTag = (imageName[..idx], imageName[(idx + 1)..]);
        return true;
    }

    public static string JoinImageNameAndTag(string imageName, string? tag)
    {
        return tag == null ? imageName : $"{imageName}:{tag}";
    }
}