using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ServerName : MonoBehaviour {
    public FpsServer.GameServer m_server;
    public Text serverText;
    public void GetRegionName()
    {
        m_server.RegionServerName = serverText.text;
        m_server.SendRegisterServer();
    }
}
