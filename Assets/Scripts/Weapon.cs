using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;
using UnityEngine.UIElements;
using Photon.Pun;
using System;
using TMPro;

public class Weapon : MonoBehaviourPunCallbacks
{
    public static Weapon Instance;
    public Vector3 hitDir = Vector3.zero;

    PhotonView PV;

    public Gun[] loadout;
    public Transform weaponParent;

    private int currentIndex;

    private GameObject currentWeapon;

    public GameObject bulletHolePrefab;
    public ParticleSystem bulletParticle;
    public LayerMask canBeShot;

    private bool isReloading = false;

    private float currentCoolDown;
    private float currentInterval;

    void Awake()
    {
        PV = GetComponent<PhotonView>();
        Instance = this;
    }

    private void Start()
    {
        //if(PV.IsMine) PV.RPC("Equip", RpcTarget.All, 0);
        foreach (Gun a in loadout) a.Initialize();
        Equip(0);
    }

    void Update()
    {
        if (PV.IsMine && Input.GetKeyDown(KeyCode.Alpha1))
        {
            PV.RPC("Equip", RpcTarget.All, 0);
        }

        if (currentWeapon != null)
        {
            if (PV.IsMine)
            {
                
                Aim(Input.GetMouseButton(1));
                if (Input.GetMouseButtonDown(0) && currentCoolDown <= 0 && !loadout[currentIndex].holdMouse)
                {
                    if (loadout[currentIndex].FireBullet()) PV.RPC("Shoot", RpcTarget.All);
                    else StartCoroutine(Reload(loadout[currentIndex].reloadTime));
                }
                else if (Input.GetMouseButton(0) && currentCoolDown <= 0 && loadout[currentIndex].holdMouse)
                {
                    if (loadout[currentIndex].interval != 0) currentInterval = Mathf.Ceil(Mathf.Sin((Mathf.PI / loadout[currentIndex].interval) * Time.fixedTime));
                    else currentInterval = 1;
                    Debug.Log(currentInterval);
                    if (currentInterval > 0)
                    {
                        if (loadout[currentIndex].FireBullet()) PV.RPC("Shoot", RpcTarget.All);
                        else StartCoroutine(Reload(loadout[currentIndex].reloadTime));
                    }
                }

                if (Input.GetKeyDown(KeyCode.R)) { StartCoroutine(Reload(loadout[currentIndex].reloadTime)); }

                //cooldown
                if (currentCoolDown > 0) currentCoolDown -= Time.deltaTime;
            }

            currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 6);

        }

    }

    [PunRPC]
    void Equip(int p_ind)
    {
        if (currentWeapon != null)
        {
            if(isReloading) StopCoroutine("Reload");
            Destroy(currentWeapon);
        }

        currentIndex = p_ind;

        GameObject t_newWeapon = Instantiate(loadout[p_ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
        t_newWeapon.transform.localPosition = Vector3.zero;
        t_newWeapon.transform.localEulerAngles = Vector3.zero;

        t_newWeapon.GetComponent<Sway>().isMine = PV.IsMine;

        currentWeapon = t_newWeapon;
    }

    void Aim(bool _isAiming)
    {
        Transform t_ads = currentWeapon.transform.Find("States/ADS");
        Transform t_anchor = currentWeapon.transform.Find("Anchor");
        Transform t_hip = currentWeapon.transform.Find("States/Hip");

        if (_isAiming)
        {
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_ads.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        }
        else
        {
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_hip.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        }        
    }

    [PunRPC]
    void Shoot()
    {
        Transform t_spawn = transform.Find("Cameras/CamHolder");
        //bloom
        Vector3 t_bloom = t_spawn.position + t_spawn.forward * 10000f;
        t_bloom += UnityEngine.Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.up;
        t_bloom += UnityEngine.Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.right;
        t_bloom.Normalize();

        //cooldown
        currentCoolDown = loadout[currentIndex].fireRate;

        RaycastHit t_hit = new RaycastHit();
        if (Physics.Raycast(t_spawn.position, t_bloom, out t_hit, 1000f, canBeShot))
        {
            hitDir = t_hit.normal;
            GameObject t_newHole = null;
            if (t_hit.collider.gameObject.layer != 11)
            {
                t_newHole = Instantiate(bulletHolePrefab, t_hit.point + t_hit.normal * .001f, Quaternion.identity) as GameObject;
                t_newHole.transform.LookAt(t_hit.point + t_hit.normal);
            }
            ParticleSystem t_newBP = Instantiate(bulletParticle, t_hit.point + t_hit.normal * .001f, Quaternion.identity) as ParticleSystem;
            t_newBP.transform.LookAt(t_hit.point + t_hit.normal);
            var mainBP = t_newBP.main;
            if (t_hit.collider.gameObject.GetComponent<Renderer>()) mainBP.startColor = t_hit.collider.gameObject.GetComponent<Renderer>().material.color;
            else if (t_hit.collider.gameObject.layer == 11) mainBP.startColor = Color.red;

            if (t_newHole != null) Destroy(t_newHole, 5f);
            Destroy(t_newBP, 5f);

            if (PV.IsMine)
            {
                //shootin other player
                if(t_hit.collider.gameObject.layer == 11)
                {
                    //RPC for damage
                    t_hit.collider.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, loadout[currentIndex].damage);
                }
            }

        }

        //gun fx
        currentWeapon.transform.Rotate(-loadout[currentIndex].recoil, 0, 0);
        currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[currentIndex].kickback;

    }

    [PunRPC]
    private void TakeDamage(int p_damage)
    {
        GetComponent<PlayerController>().TakeDamage(p_damage);
    }

    IEnumerator Reload(float p_wait)
    {
        isReloading = true;
        currentWeapon.SetActive(false);

        yield return new WaitForSeconds(p_wait);

        loadout[currentIndex].Reload();
        currentWeapon.SetActive(true);
        isReloading = false;
    }

    public void RefreshAmmo(TMP_Text p_text)
    {
        int t_clip = loadout[currentIndex].GetClip();
        int t_stash = loadout[currentIndex].GetStash();

        p_text.text = t_clip.ToString() + " / " +  t_stash.ToString();
    }

}
