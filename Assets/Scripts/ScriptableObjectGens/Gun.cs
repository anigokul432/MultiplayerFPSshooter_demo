using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Gun", menuName = "Gun")]
public class Gun : ScriptableObject
{
    public string Name;
    public int damage;
    public float fireRate;
    public float reloadTime;
    public float bloom;
    public float recoil;
    public float kickback;
    public float aimSpeed;
    public float interval;
    public bool holdMouse;
    public GameObject prefab;

    public int ammo;
    public int clipSize;

    private int clip, stash; //current

    public void Initialize()
    {
        stash = ammo;
        clip = clipSize;
    }

    public bool FireBullet()
    {
        if (clip > 0)
        {
            clip -= 1;
            return true;
        }
        else return false;
    }

    public void Reload()
    {
        stash += clip;
        clip = Mathf.Min(clipSize, stash);
        stash -= clip;
    }

    public int GetStash() { return stash; }
    public int GetClip() { return clip; }


}
