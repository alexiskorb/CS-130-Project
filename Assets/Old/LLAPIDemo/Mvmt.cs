using UnityEngine;
using System.Collections;
//Written to prototype the LLAPI for Unet. Code is based on tutorials from Unity's documentation and
//a tutorial from Jonathon Merefield.

//Character movement code using the LLAPI. 
public class Mvmt : MonoBehaviour
{
	public LLAPIClient client;

    private void Start()
    {
    }

    //Read state of the object, then use the client sendMessage code to send coordinates to server.
    private void Update()
    {
        float xPos = Input.GetAxis("Horizontal");
        float yPos = Input.GetAxis("Vertical");
  //      transform.Translate(xPos, 0, yPos);

        if (xPos != 0 || yPos != 0)
        {
            string msg = "MV|" + xPos.ToString() + "|" + yPos.ToString();
            client.sendMessage(msg);
        }
			
		//transform.Translate(client.currentPlayerPos);
    }
}