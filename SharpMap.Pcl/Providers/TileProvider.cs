﻿// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

// SOURCECODE IS MODIFIED FROM SharpMap-SL-80096 BY ITACASOFT DI VITA FABRIZIO
// 22/02/2011: excluded what does not compile under Windows Phone

using System;
using System.Collections.Generic;
using System.Threading;
#if !WINDOWS_PHONE && !PCL
using BruTile;
#endif
using SharpMap.Data;
using SharpMap.Data.Providers;
using GeoAPI.Geometries;
using System.Collections.ObjectModel;
#if !WINDOWS_PHONE && !PCL
using BruTile.Cache;
#endif
using System.Linq;

namespace SharpMap.Providers
{
#if !WINDOWS_PHONE && !PCL
    public class TileProvider : IProvider
    {
    #region Fields

        double minVisible = Double.MinValue;
        double maxVisible = Double.MaxValue;
        bool enabled = true;
        string layerName;
        int srid;
        ITileSource source;
        MemoryCache<byte[]> bitmaps = new MemoryCache<byte[]>(100, 200);
        List<TileIndex> queue = new List<TileIndex>();

        #endregion

    #region Properties

        public double MinVisible
        {
            get { return minVisible; }
            set { minVisible = value; }
        }

        public double MaxVisible
        {
            get { return maxVisible; }
            set { maxVisible = value; }
        }

        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        public string LayerName
        {
            get { return layerName; }
            set { layerName = value; }
        }

        public BoundingBox GetExtents()
        {
            return this.source.Schema.Extent.ToBoundingBox();
        }

        public int SRID
        {
            get { return srid; }
            set { srid = value; }
        }

        #endregion

        public TileProvider(ITileSource tileSource, string layerName)
        {
            this.source = tileSource;
            this.layerName = layerName;
        }

        public IFeatures FetchTiles(BoundingBox boundingBox, double resolution)
        {
            Extent extent = new Extent(boundingBox.Min.X, boundingBox.Min.Y, boundingBox.Max.X, boundingBox.Max.Y);
            int level = BruTile.Utilities.GetNearestLevel(source.Schema.Resolutions, resolution);
            IList<TileInfo> tiles = source.Schema.GetTilesInView(extent, level);

            ICollection<WaitHandle> waitHandles = new List<WaitHandle>();
                        
            foreach (TileInfo info in tiles)    
            {
                if (bitmaps.Find(info.Index) != null) continue;
                if (queue.Contains(info.Index)) continue;
                AutoResetEvent waitHandle = new AutoResetEvent(false);
                waitHandles.Add(waitHandle);
                queue.Add(info.Index);

                Thread thread = new Thread(GetTileOnThread);
                thread.Start(new object[] { source.Provider, info, bitmaps, waitHandle });
                //!!!ThreadPool.QueueUserWorkItem(GetTileOnThread, new object[] { source.Provider, info, bitmaps, waitHandle });
            }

            //foreach (WaitHandle handle in waitHandles)
            //    handle.WaitOne();

            IFeatures features = new Features();
            foreach (TileInfo info in tiles)
            {
                byte[] bitmap = bitmaps.Find(info.Index);
                if (bitmap == null) continue;
                IRaster raster = new Raster(bitmap, new BoundingBox(info.Extent.MinX, info.Extent.MinY, info.Extent.MaxX, info.Extent.MaxY));
                IFeature feature = features.New();
                feature.Geometry = raster;
                features.Add(feature);
            }
            return features;
        }
        
        private void GetTileOnThread(object parameter)
        {
            object[] parameters = (object[])parameter;
            if (parameters.Length != 4) throw new ArgumentException("Four parameters expected");
            ITileProvider tileProvider = (ITileProvider)parameters[0];
            TileInfo tileInfo = (TileInfo)parameters[1];
            MemoryCache<byte[]> bitmaps = (MemoryCache<byte[]>)parameters[2];
            AutoResetEvent autoResetEvent = (AutoResetEvent)parameters[3];

            try
            {
                bitmaps.Add(tileInfo.Index, tileProvider.GetTile(tileInfo));
            }
            catch (Exception ex)
            {
                //todo: log and use other ways to report to user.
            }
            finally
            {
                queue.Remove(tileInfo.Index);
                autoResetEvent.Set();
            }
        }

    #region IRasterProvider Members

        public IFeatures GetFeaturesInView(BoundingBox box, double resolution)
        {
            return FetchTiles(box, resolution);
        }

        #endregion

    #region IProvider Members

        public string ConnectionID
        {
            get { return String.Empty; }
        }

        public bool IsOpen
        {
            get { return true; }
        }
                
        public void Open()
        {
            //TODO: redesign so that methods like these are not necessary if not implemented
        }

        public void Close()
        {
            //TODO: redesign so that methods like these are not necessary if not implemented
        }

        #endregion

    #region IDisposable Members

        public void Dispose()
        {
            //nothing to dispose
        }

        #endregion
    }
#endif
}

#if !WINDOWS_PHONE && !PCL
namespace BruTile
{
    static class BruTileExtensions
    {
        public static BoundingBox ToBoundingBox(this Extent extent)
        {
            return new BoundingBox(
                extent.MinX,
                extent.MinY,
                extent.MaxX,
                extent.MaxY);
        }
    }
}
#endif