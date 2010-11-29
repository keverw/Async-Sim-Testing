/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Services.Interfaces;
using OpenSim.Server.Base;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.Connectors
{
    public class NeighbourServicesConnector : INeighbourService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        protected IGridService m_GridService = null;
        protected Dictionary<UUID, List<GridRegion>> m_KnownNeighbors = new Dictionary<UUID, List<GridRegion>>();

        public Dictionary<UUID, List<GridRegion>> Neighbors
        {
            get { return m_KnownNeighbors; }
        }

        public NeighbourServicesConnector()
        {
        }

        public NeighbourServicesConnector(IGridService gridServices)
        {
            Initialise(gridServices);
        }

        public virtual void Initialise(IGridService gridServices)
        {
            m_GridService = gridServices;
        }

        public virtual List<GridRegion> InformNeighborsThatRegionisUp(RegionInfo incomingRegion)
        {
            return new List<GridRegion>();
        }

        public List<GridRegion> InformNeighborsRegionIsUp(RegionInfo incomingRegion, List<GridRegion> alreadyInformedRegions)
        {
            List<GridRegion> informedRegions = new List<GridRegion>();
            foreach (GridRegion neighbor in Neighbors[incomingRegion.RegionID])
            {
                //If we have already informed the region, don't tell it again
                if (alreadyInformedRegions.Contains(neighbor))
                    continue;
                //Call the region then and add the regions it informed
                informedRegions.AddRange(DoHelloNeighbourCall(neighbor, incomingRegion));
            }
            return informedRegions;
        }

        public List<GridRegion> DoHelloNeighbourCall(GridRegion region, RegionInfo thisRegion)
        {
            List<GridRegion> informedRegions = new List<GridRegion>();
            string uri = "http://" + region.ExternalEndPoint.Address + ":" + region.HttpPort + "/region/" + thisRegion.RegionID + "/";
            //m_log.Debug("   >>> DoHelloNeighbourCall <<< " + uri);

            // Fill it in
            OSDMap args = null;
            try
            {
                args = thisRegion.PackRegionInfoData();
            }
            catch (Exception e)
            {
                m_log.Debug("[REST COMMS]: PackRegionInfoData failed with exception: " + e.Message);
                return informedRegions;
            }

            string queryString = ServerUtils.BuildQueryString(Util.OSDToDictionary(args));
            string reply = SynchronousRestFormsRequester.MakeRequest("POST", uri, queryString);

            if (reply == "")
                return informedRegions;

            OSDMap replyMap = Util.DictionaryToOSD(ServerUtils.ParseXmlResponse(reply));

            try
            {
                if (replyMap == null)
                    return new informedRegions;

                //Didn't inform, return now
                if (!replyMap.ContainsKey("success") || !replyMap["success"].AsBoolean())
                    return informedRegions;

                foreach (KeyValuePair<string, OSD> kvp in replyMap)
                {
                    if (kvp.Value is OSDMap)
                    {
                        OSDMap r = kvp.Value as OSDMap;
                        GridRegion nregion = new GridRegion(Util.OSDToDictionary(r));
                        informedRegions.Add(nregion);
                    }
                }
            }
            catch(Exception ex)
            {
                m_log.Warn("[NeighborServiceConnector]: Failed to read response from neighbor " + ex.ToString());
            }

            return informedRegions;
        }
    }
}
