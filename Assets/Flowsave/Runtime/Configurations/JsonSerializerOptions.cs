namespace Flowsave.Configurations
{
    [System.Serializable]
    public class JsonSerializerOptions
    {
        public bool prettyPrint = false;
        public bool includeNulls = true;
        public bool typeHinting = true; // True for full type hinting, false for simple
    }

}