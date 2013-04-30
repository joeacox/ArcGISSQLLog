using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArcGISSQLLog
{
    public class Layer
    {
        public int id { get; set; }
        public string name { get; set; }
        public int parentLayerId { get; set; }
        public bool defaultVisibility { get; set; }
        public object subLayerIds { get; set; }
        public double minScale { get; set; }
        public double maxScale { get; set; }
    }

    public class SpatialReference
    {
        public int wkid { get; set; }
        public int latestWkid { get; set; }
    }

    public class InitialExtent
    {
        public double xmin { get; set; }
        public double ymin { get; set; }
        public double xmax { get; set; }
        public double ymax { get; set; }
        public SpatialReference spatialReference { get; set; }
    }

    public class FullExtent
    {
        public double xmin { get; set; }
        public double ymin { get; set; }
        public double xmax { get; set; }
        public double ymax { get; set; }
        public SpatialReference spatialReference { get; set; }
    }

    public class DocumentInfo
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Comments { get; set; }
        public string Subject { get; set; }
        public string Category { get; set; }
        public string AntialiasingMode { get; set; }
        public string TextAntialiasingMode { get; set; }
        public string Keywords { get; set; }
    }

    public class MapServer
    {
        public double currentVersion { get; set; }
        public string serviceDescription { get; set; }
        public string mapName { get; set; }
        public string description { get; set; }
        public string copyrightText { get; set; }
        public bool supportsDynamicLayers { get; set; }
        public List<Layer> layers { get; set; }
        public List<object> tables { get; set; }
        public SpatialReference spatialReference { get; set; }
        public bool singleFusedMapCache { get; set; }
        public InitialExtent initialExtent { get; set; }
        public FullExtent fullExtent { get; set; }
        public int minScale { get; set; }
        public int maxScale { get; set; }
        public string units { get; set; }
        public string supportedImageFormatTypes { get; set; }
        public DocumentInfo documentInfo { get; set; }
        public string capabilities { get; set; }
        public string supportedQueryFormats { get; set; }
        public bool hasVersionedData { get; set; }
        public int maxRecordCount { get; set; }
        public int maxImageHeight { get; set; }
        public int maxImageWidth { get; set; }
    }
}
