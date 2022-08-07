using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
	#region Variables

	[SerializeField] GameObject cameraHolder;
	[SerializeField] GameObject model;
	[SerializeField] GameObject handModel;
	[SerializeField] float counterMovement;
	[SerializeField] Transform weapon;
	[SerializeField] bool canCrouch;
	[SerializeField] Transform IKtarget;
	private Vector3 weaponParentOrigin;
	private float movementCounter, idleCounter;

	[SerializeField] LayerMask groundMask;

	Animator anim;

	[SerializeField] float mouseSensitivity, maxVelocityChange, runSpeed, jumpHeight;
	float speed;
	Vector3 velocity, targetVelocity, velocityChange;
	bool canJump = true;
	float gravity = 9.8f;
	float height;

	public float bobSpeed;

	float pitch, yaw;
	bool grounded;
	Vector3 smoothMoveVelocity;
	Vector3 moveAmount;

	bool isInput;
	Rigidbody rb;

	PhotonView PV;

	float x, y;
	bool jumping, crouching;
	#endregion

	#region UI Variables
	public int maxHealth;
	private int currentHealth;
	private Transform UIhealthBar;

	private TMP_Text UI_Ammo;
	private Transform dead;
	#endregion

	void Awake()
	{
		rb = GetComponent<Rigidbody>();
		PV = GetComponent<PhotonView>();

		rb.useGravity = false;
		rb.freezeRotation = true;
		height = GetComponent<CapsuleCollider>().height;
	}

	void Start()
	{
		cameraHolder.SetActive(PV.IsMine);
		currentHealth = maxHealth;

		weaponParentOrigin = weapon.localPosition;
		/*
		foreach (GameObject c in GameObject.FindGameObjectsWithTag("bones"))
		{
			Physics.IgnoreCollision(GetComponent<Collider>(), c.GetComponent<Collider>());
			c.GetComponent<Rigidbody>().isKinematic = true;
		}
		*/
		if (PV.IsMine)
		{
			foreach (SkinnedMeshRenderer i in model.GetComponentsInChildren<SkinnedMeshRenderer>()) i.enabled = false;
			foreach (MeshRenderer i in model.GetComponentsInChildren<MeshRenderer>()) i.enabled = false;

			foreach (MeshRenderer i in handModel.GetComponentsInChildren<MeshRenderer>()) i.enabled = true;
			foreach (SkinnedMeshRenderer i in handModel.GetComponentsInChildren<SkinnedMeshRenderer>()) i.enabled = true;
			anim = GetComponentInChildren<Animator>();

			UIhealthBar = GameObject.Find("HUD/Health/Bar").transform;
			UI_Ammo = GameObject.Find("HUD/Ammo/Text").GetComponent<TMP_Text>();

			Weapon.Instance.RefreshAmmo(UI_Ammo);
			RefreshHealthBar();
		}
		else
		{
			foreach (SkinnedMeshRenderer i in model.GetComponentsInChildren<SkinnedMeshRenderer>()) i.enabled = true;
			foreach (MeshRenderer i in model.GetComponentsInChildren<MeshRenderer>()) i.enabled = true;

			foreach (MeshRenderer i in handModel.GetComponentsInChildren<MeshRenderer>()) i.enabled = false;
			foreach (SkinnedMeshRenderer i in handModel.GetComponentsInChildren<SkinnedMeshRenderer>()) i.enabled = false;
			
			gameObject.layer = 11;

		}
/*
		if (!PV.IsMine)
		{
			if(rb.transform.root.name != "Dead") Destroy(rb);
		}
*/

	}

	void Look()
	{
		yaw = Input.GetAxisRaw("Mouse X");
		transform.Rotate(Vector3.up * yaw * mouseSensitivity);

		pitch += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
		pitch = Mathf.Clamp(pitch, -90f, 90f);

		cameraHolder.transform.localEulerAngles = weapon.transform.localEulerAngles = Vector3.left * pitch;
		
	}

	void Movement()
	{
		// Calculate how fast we should be moving
		
	    targetVelocity = new Vector3(x, 0, y).normalized;
		Vector3 temp = targetVelocity;
		targetVelocity = transform.TransformDirection(targetVelocity);
		targetVelocity *= speed;

		// Apply a force that attempts to reach our target velocity
		velocity = rb.velocity;
		velocityChange = (targetVelocity - velocity);
		velocityChange.x = Mathf.Clamp(velocityChange.x, -maxVelocityChange, maxVelocityChange);
		velocityChange.z = Mathf.Clamp(velocityChange.z, -maxVelocityChange, maxVelocityChange);
		velocityChange.y = 0;

		rb.AddForce(velocityChange * (grounded && !Input.GetKey(KeyCode.Space) ? 10 : 2f), ForceMode.Force);
	}

	public float fallMultiplier = 2.5f;
	public float lowJumpMultiplier = 2f;

	private void Jump()
	{
		if (grounded)
		{
			// Jump
			if (canJump && Input.GetButton("Jump"))
			{
				rb.velocity = new Vector3(velocity.x, CalculateJumpVerticalSpeed(), velocity.z);
			}
		}

		if(rb.velocity.y < 0)
		{
			rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
		}
	}

	float CalculateJumpVerticalSpeed()
	{
		// From the jump height and gravity we deduce the upwards speed 
		// for the character to reach at the apex.
		return Mathf.Sqrt(2 * jumpHeight * gravity);
	}

	void StartCrouch()
	{
		canJump = false;
		GetComponent<CapsuleCollider>().height = height / 2;
		transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
	}

	void StopCrouch()
	{
		canJump = true;
		transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
		GetComponent<CapsuleCollider>().height = height;
	}
	
	void Animations()
	{
		if (grounded)
		{
			anim.speed = speed / 7;
			anim.SetFloat("Horizontal", x , .08f, Time.deltaTime);
			anim.SetFloat("Vertical", y , .08f, Time.deltaTime);
			anim.SetBool("grounded", true);
		}
		else
		{
			anim.SetBool("grounded", false);
		}
	}

	private void LateUpdate()
	{
		if (!PV.IsMine)
			return;
		
	}

	void FixedUpdate()
	{
		if (!PV.IsMine)
			return;
		isInput = Input.GetKey("w") || Input.GetKey("d") || Input.GetKey("s") || Input.GetKey("a");

		Movement();
		speed = runSpeed;
		if (canJump) Jump();

		//gravity
		rb.AddForce(new Vector3(0, -gravity * rb.mass, 0));
	}

	void Update()
	{
		if (!PV.IsMine)
			return;

		Look();

		Weapon.Instance.RefreshAmmo(UI_Ammo);

		x = Input.GetAxisRaw("Horizontal");
		y = Input.GetAxisRaw("Vertical");
		jumping = Input.GetButton("Jump");
		crouching = Input.GetKey(KeyCode.LeftControl);



		//Crouching
		if (canCrouch)
		{
			if (crouching) speed = runSpeed / 2.5f;
			else speed = runSpeed;

			if (Input.GetKeyDown(KeyCode.LeftControl))
				StartCrouch();
			if (Input.GetKeyUp(KeyCode.LeftControl))
				StopCrouch();
		}

		//Headbob
		if (y == 0 && x == 0) { HeadBob(idleCounter, .005f, .005f); idleCounter += Time.deltaTime; }
		else { HeadBob(movementCounter, .01f, .01f); movementCounter += Time.deltaTime * 6f * (speed/runSpeed) * bobSpeed; }
		weapon.localPosition = Vector3.Lerp(weapon.localPosition, targetWeaponBobPos, 12 * Time.deltaTime);


		//damage test
		if (Input.GetKeyDown(KeyCode.U))
		{
			TakeDamage(5);
		}

		Animations();
		GroundCheck();

		RefreshHealthBar();
		IK();
	}

	void GroundCheck()
	{
		grounded = Physics.CheckCapsule(GetComponent<CapsuleCollider>().bounds.center, new Vector3(GetComponent<CapsuleCollider>().bounds.center.x, GetComponent<CapsuleCollider>().bounds.min.y - 0.1f, GetComponent<CapsuleCollider>().bounds.center.z), 0.18f);
	}

	private Vector3 targetWeaponBobPos;
	void HeadBob(float _z, float _xIntensity, float _yIntensity)
	{
		if (grounded)
		{
			targetWeaponBobPos = weaponParentOrigin + new Vector3(x * Mathf.Cos(_z) * _yIntensity, Mathf.Sin(2 * _z) * _yIntensity,y* Mathf.Cos(_z) * _yIntensity);		}
		else
		{
			targetWeaponBobPos = weaponParentOrigin + new Vector3(0, .025f, 0);
		}
	}


	//health
	public void TakeDamage(int p_damage)
	{

		if (PV.IsMine)
		{
			currentHealth -= p_damage;
			//IKtarget.localPosition -= transform.forward * .1f;
			RefreshHealthBar();

			if(currentHealth <= 0)
			{				
				PhotonNetwork.Destroy(gameObject);
				string _name = gameObject.name;
				PlayerManager.Instance.CreateDeadBody(_name.Substring(0, _name.Length - 7), transform.position, transform.rotation);

				PlayerManager.Instance.Spawn();

				//cursor
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}
	}

	void RefreshHealthBar()
	{
		float t_healthRatio = (float)currentHealth / (float)maxHealth;
		UIhealthBar.localScale = Vector3.Lerp(UIhealthBar.localScale, new Vector3(t_healthRatio, 1, 1), 6 * Time.deltaTime);
	}


	void IK()
	{
		//IKtarget.localPosition = Vector3.Lerp(IKtarget.localPosition, Vector3.zero, 10 * Time.deltaTime);
	}

	
	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(pitch);
			stream.SendNext(yaw);
			stream.SendNext(rb.position);
			stream.SendNext(rb.rotation);
			stream.SendNext(rb.velocity);
		}
		else
		{
			pitch = (float)stream.ReceiveNext();
			yaw = (float)stream.ReceiveNext();
			cameraHolder.transform.localEulerAngles = weapon.transform.localEulerAngles = Vector3.left * pitch;

			rb.position = (Vector3)stream.ReceiveNext();
			rb.rotation = (Quaternion)stream.ReceiveNext();
			rb.velocity = (Vector3)stream.ReceiveNext();

			float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
			rb.position += rb.velocity * lag;
		}
	}
	
	
}