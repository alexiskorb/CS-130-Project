using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDisplay : MonoBehaviour {

    // Sprite for displaying life
    public Sprite lifeSprite;
    // Sprite for displaying a lost life
    public Sprite lostLifeSprite;

    private List<GameObject> m_lifeSpriteObjects;
    private int m_maxLife;


	void Awake () {
        m_lifeSpriteObjects = new List<GameObject>();
	}

    private void Start()
    {
        SetMaxLife(m_maxLife);
    }

    // Update is called once per frame
    void Update () {
        // Set the player display to look at the main camera
        Camera camera = Camera.main;
        if (camera != null)
        {
            transform.LookAt(transform.position + camera.transform.rotation * Vector3.forward, camera.transform.rotation * Vector3.up);
        }
    }

    // Set the maximum number of lives and current number of lives shown in the display
    // If the current life is not specified, it is set to max life
    public bool SetMaxLife(int maxLife, int currentLife = -1)
    {
        if (currentLife < 0)
        {
            currentLife = maxLife;
        }
        if (maxLife > 0)
        {
            m_maxLife = maxLife;

            // Destroy old life objects
            foreach (GameObject lifeObject in m_lifeSpriteObjects)
            {
                Destroy(lifeObject);
            }
            m_lifeSpriteObjects.Clear();

            // Add in new life objects
            for (int i = 0; i < maxLife; i++)
            {
                GameObject lifeObject = new GameObject(); 
                Image imageComponent = lifeObject.AddComponent<Image>();
                imageComponent.sprite = lifeSprite;
                imageComponent.preserveAspect = true;
                lifeObject.GetComponent<RectTransform>().SetParent(this.transform); 
                lifeObject.SetActive(true);
                m_lifeSpriteObjects.Add(lifeObject);
            }

            SetCurrentLife(currentLife);
            return true;
        }
        return false;
    }

    public bool SetCurrentLife(int life)
    {
       if (life >= 0 && life <= m_maxLife)
       {
            for (int i = 0; i < life; i++)
            {
                m_lifeSpriteObjects[i].GetComponent<Image>().sprite = lifeSprite;
            }
            for (int i = life; i < m_maxLife; i++)
            {
                m_lifeSpriteObjects[i].GetComponent<Image>().sprite = lostLifeSprite;
            }
            return true;
       }
       return false;
    }
}
