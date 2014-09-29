using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{
    public class TimeWarpDecorator
    {
        /// <summary>
        /// Craped Timewarpobject from stock
        /// </summary>
        private TimeWarp mTimewarpObject;
        /// <summary>
        /// Delay-Text style
        /// </summary>
        private GUIStyle mTextStyle;
        // Diagnostics text, green style
        private GUIStyle mGreenTextStyle;
        // Diagnostics text, red style
        private GUIStyle mRedTextStyle;
        // Diagnostic text, target style
        private GUIStyle mTargetTextStyle;
        // Diagnostics display toggle
        private bool mDisplayRoute;

        /// <summary>
        /// Green Flightcomputer button
        /// </summary>
        private readonly GUIStyle mFlightButtonGreen;
        /// <summary>
        /// Red Flightcomputer button
        /// </summary>
        private readonly GUIStyle mFlightButtonRed;
        /// <summary>
        /// Yellow Flightcomputer button
        /// </summary>
        private readonly GUIStyle mFlightButtonYellow;
        /// <summary>
        /// Delay-Timer Background
        /// </summary>
        private readonly Texture2D mTexBackground;
        /// <summary>
        /// Activ Vessel
        /// </summary>
        private VesselSatellite mVessel { get{  return RTCore.Instance.Satellites[FlightGlobals.ActiveVessel]; }  }



        private String DisplayText
        {
            get
            {
                var vs = this.mVessel;
                if (vs == null)
                {
                    return "N/A";
                }
                else if (vs.HasLocalControl)
                {
                    return "Local Control";
                }
                else if (vs.Connections.Any())
                {
                    if (RTSettings.Instance.EnableSignalDelay)
                    {
                        return "D+ " + vs.Connections[0].Delay.ToString("F6") + "s";
                    }
                    else
                    {
                        return "Connected";
                    }                    
                }
                return "No Connection";
            }
        }

        /// <summary>
        /// Returns the Style of the Flightcomputer button by the connected status
        /// </summary>
        private GUIStyle ButtonStyle
        {
            get
            {
                var vs = this.mVessel;
                if (vs == null) 
                {
                    return mFlightButtonRed;
                }
                else if (vs.HasLocalControl)
                {
                    return mFlightButtonYellow;
                }
                else if (vs.Connections.Any())
                {
                    return mFlightButtonGreen;
                }
                return mFlightButtonRed;
            }
        }
        
        public TimeWarpDecorator()
        {
            mFlightButtonGreen = GUITextureButtonFactory.CreateFromFilename("texFlightGreen.png","texFlightGreenOver.png","texFlightGreenDown.png","texFlightGreenOver.png");
            mFlightButtonYellow = GUITextureButtonFactory.CreateFromFilename("texFlightYellow.png","texFlightYellowOver.png","texFlightYellowDown.png","texFlightYellowOver.png");
            mFlightButtonRed = GUITextureButtonFactory.CreateFromFilename("texFlightRed.png","texFlightRed.png","texFlightRed.png","texFlightRed.png");

            mFlightButtonGreen.fixedHeight = mFlightButtonGreen.fixedWidth = 0;
            mFlightButtonYellow.fixedHeight = mFlightButtonYellow.fixedWidth = 0;
            mFlightButtonRed.fixedHeight = mFlightButtonRed.fixedWidth = 0;
            mFlightButtonGreen.stretchHeight = mFlightButtonGreen.stretchWidth = true;
            mFlightButtonYellow.stretchHeight = mFlightButtonYellow.stretchWidth = true;
            mFlightButtonRed.stretchHeight = mFlightButtonRed.stretchWidth = true;

            // Crap timewarp object
            mTimewarpObject = TimeWarp.fetch;

            // objects on this scene?
            if (mTimewarpObject == null || mTimewarpObject.timeQuadrantTab == null)
            {
                // to skip the draw calls
                mTimewarpObject = null;
                return;
            }

            var text = mTimewarpObject.timeQuadrantTab.transform.FindChild("MET timer").GetComponent<ScreenSafeGUIText>();
            mTextStyle = new GUIStyle(text.textStyle);
            mTextStyle.fontSize = (int)(text.textSize * ScreenSafeUI.PixelRatio);

            mGreenTextStyle = new GUIStyle(mTextStyle);
            mGreenTextStyle.normal.textColor = Color.green;
            mGreenTextStyle.alignment = TextAnchor.LowerRight;

            mRedTextStyle = new GUIStyle(mTextStyle);
            mRedTextStyle.normal.textColor = Color.red;
            mRedTextStyle.alignment = TextAnchor.LowerRight;

            mTargetTextStyle = new GUIStyle(mTextStyle);
            mTargetTextStyle.normal.textColor = Color.green;
            mTargetTextStyle.alignment = TextAnchor.LowerLeft;

            // Put the draw function to the DrawQueue
            RenderingManager.AddToPostDrawQueue(0, Draw);

            // create the Background
            RTUtil.LoadImage(out mTexBackground, "TimeQuadrantFcStatus.png");
        }

        private void DrawSignalRoute(float x, float y)
        {
            float distanceWidth = 80;
            float antennaWidth = 40;
            float targetWidth = 250;
            float areaWidth = distanceWidth + antennaWidth + antennaWidth + targetWidth;
            float lineHeight = 20;

            var satellite = RTCore.Instance.Satellites[FlightGlobals.ActiveVessel];
            var source = satellite as ISatellite;
            NetworkRoute<ISatellite> connection = null;

            // If there are no connections, or an infinite delay connection, examine the LastConnection
            // other wise use the currently shortest connection
            if (RTCore.Instance.Network[satellite].Count == 0 ||
                Double.IsPositiveInfinity(RTCore.Instance.Network[satellite][0].Delay))
                connection = RTCore.Instance.Network.LastConnection(satellite);
            else
                connection = RTCore.Instance.Network[satellite][0];

            if(connection == null)
                GUILayout.BeginArea(new Rect(x, y, areaWidth, lineHeight * 2));
            else
                GUILayout.BeginArea(new Rect(x, y, areaWidth, lineHeight * (connection.Links.Count + 1)));

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Distance", mGreenTextStyle,  GUILayout.Width(distanceWidth));
            GUILayout.Label("TX",       mGreenTextStyle,  GUILayout.Width(antennaWidth));
            GUILayout.Label("RX",       mGreenTextStyle,  GUILayout.Width(antennaWidth));
            GUILayout.Space(10);
            GUILayout.Label("Target",   mTargetTextStyle, GUILayout.Width(targetWidth));
            GUILayout.EndHorizontal();

            if (connection != null && connection.Links != null && connection.Links.Count > 0)
            {
                foreach (var link in connection.Links)
                    {
                    GUILayout.BeginHorizontal();

                    // Distance
                    if (source.DistanceTo(link.Target) > Math.Min(link.Transmitters.Max(a => (a.Omni != 0) ? a.Omni : a.Dish), 
                        link.Receivers.Max(a => (a.Omni != 0) ? a.Omni : a.Dish)))
                        GUILayout.Label(RTUtil.FormatSI(source.DistanceTo(link.Target), "m"),   mRedTextStyle, GUILayout.Width(distanceWidth));
                    else
                        GUILayout.Label(RTUtil.FormatSI(source.DistanceTo(link.Target), "m"), mGreenTextStyle, GUILayout.Width(distanceWidth));

                    // Transmitter status
                    // TODO: rework for non-Standard range models
                    if (!source.Antennas.Any(a => a.Powered && a.Activated))
                        GUILayout.Label("OFF",  mRedTextStyle, GUILayout.Width(antennaWidth));
                    else if (!source.HasLineOfSightWith(link.Target))
                        GUILayout.Label("LOS",  mRedTextStyle, GUILayout.Width(antennaWidth));
                    else if (!link.Transmitters.Any(a => RangeModelExtensions.IsTargeting(a, link.Target, source)))
                        GUILayout.Label("AIM",  mRedTextStyle, GUILayout.Width(antennaWidth));
                    else if (link.Transmitters.Max(a => (a.Omni != 0) ? a.Omni : a.Dish) < source.DistanceTo(link.Target))
                        GUILayout.Label("RNG",  mRedTextStyle, GUILayout.Width(antennaWidth));
                    else
                        GUILayout.Label("OK", mGreenTextStyle, GUILayout.Width(antennaWidth));

                    // Receiver status
                    // TODO: rework for non-Standard range models
                    if (!link.Target.Antennas.Any(a => a.Powered && a.Activated))
                        GUILayout.Label("OFF",  mRedTextStyle, GUILayout.Width(antennaWidth));
                    else if (!link.Target.HasLineOfSightWith(source))
                        GUILayout.Label("LOS",  mRedTextStyle, GUILayout.Width(antennaWidth));
                    else if (!link.Receivers.Any(a => RangeModelExtensions.IsTargeting(a, source, link.Target)))
                        GUILayout.Label("AIM",  mRedTextStyle, GUILayout.Width(antennaWidth));
                    else if (link.Receivers.Max(a => (a.Omni != 0) ? a.Omni : a.Dish) < link.Target.DistanceTo(source))
                        GUILayout.Label("RNG",  mRedTextStyle, GUILayout.Width(antennaWidth));
                    else
                        GUILayout.Label("OK", mGreenTextStyle, GUILayout.Width(antennaWidth));

                    GUILayout.Space(10);

                    // Target of link
                    GUILayout.Label(link.Target.Name, mTargetTextStyle, GUILayout.Width(targetWidth));

                    source = link.Target;
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                // No connection information available
                GUILayout.BeginHorizontal();
                GUILayout.Label("No Connection Information", mTargetTextStyle, GUILayout.Width(areaWidth));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }



        /// <summary>
        /// Draws the TimeQuadrantFcStatus.png, Delay time and the Flightcomputerbutton under the timewarp object
        /// </summary>
        public void Draw()
        {
            // no drawing without timewarp object
            if (mTimewarpObject == null)
                return;


            Vector2 screenCoord = ScreenSafeUI.referenceCam.WorldToScreenPoint(mTimewarpObject.timeQuadrantTab.transform.position);
            float scale = ScreenSafeUI.VerticalRatio * 900.0f / Screen.height;

            float topLeftTotimeQuadrant = Screen.height - screenCoord.y;
            float texBackgroundHeight = mTexBackground.height * 0.7f / scale;
            float texBackgroundWidth = (mTimewarpObject.timeQuadrantTab.renderer.material.mainTexture.width * 0.8111f) / scale;


            Rect delaytextPosition = new Rect(9.0f / scale, topLeftTotimeQuadrant + texBackgroundHeight - 1, 125.0f / scale, 20.0f / scale);
                        
            // calc the position under the timewarp object
            Rect pos = new Rect(mTimewarpObject.transform.position.x,
                                topLeftTotimeQuadrant + texBackgroundHeight - 3.0f,
                                texBackgroundWidth, texBackgroundHeight);

            // draw the image
            GUI.DrawTexture(pos, mTexBackground);
            // draw the delay-text
            if (GUI.Button(delaytextPosition, DisplayText, mTextStyle))
            {
                mDisplayRoute = !mDisplayRoute;
            }

            // draw the flightcomputer button to the right relativ to the delaytext position
            delaytextPosition.width = 21.0f / scale;
            delaytextPosition.x += 128 / scale;

            if (GUI.Button(delaytextPosition, "", ButtonStyle))
            {
                var satellite = RTCore.Instance.Satellites[FlightGlobals.ActiveVessel];
                if (satellite == null || satellite.SignalProcessor.FlightComputer == null) return;
                satellite.SignalProcessor.FlightComputer.Window.Show();
            }

            if (mDisplayRoute)
                DrawSignalRoute(delaytextPosition.x,delaytextPosition.y+38.0f);
        }
    }
}
