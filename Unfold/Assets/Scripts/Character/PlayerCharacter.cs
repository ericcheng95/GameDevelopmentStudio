using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerData {
	public bool win;
	public string name;
}

/**
 * PlayerCharacter class
 *
 * Represents player stats and things the player can do
 */
public class PlayerCharacter : Character {

	public PlayerData data;

	public AudioClip[] attackSound;
	private Vector3 spawn;

	public float rotateSpeed;
	public Animator animator;

	public ParticleMovement trail;

	// Helps correlate user input to attack calculation
	private Touch initialTouch;

	// Statistics that persist regardless of equippable items in player's inventory
	public int baseDamage;
	public int baseMaxHealth;
	public float baseMoveSpeed;

	// Mutable player max health, which depends on baseMaxHealth and bonusMaxHealth
	public int maxHealth;

	// Player statistics gained from equippable items
	private int bonusDamage;
	private int bonusMaxHealth;
	private float bonusMoveSpeed;

	private Item weapon = null;
	private Item armor = null;
	private Item boots = null;

	/*0 for no ability, other numbers for different types of abilities*/
	private int abilityType = 0;
	/*0 for no ability, 1 for basic ability, 2 for adept ability*/
	private int abilityLevel = 0;

	/* Is player attacking normally or with ability? */
	private bool usingAbility = false;

	private GameObject floor;

	public GameObject[] weaponList;
	public GameObject[] WeaponModelList;
	public WeaponButton weaponButton;

	/* 
	 * The cooldown timer for breaking the wall 
	 * Set to -1 to disable the ability
	 * Set to 0 to activate this ability, each time hammer is used, this variable will set back to HAMMER_COOLDOWN, and decremented by 1 in each update
	 */
	public int hammerCooldown = -1;
	private int HAMMER_COOLDOWN = 100;
	public EditWalls wall;

	//used for special abilities
	private string ability;

	void Start() {
		updateStats();
		this.animator.SetBool("Walking", false);
		this.currentHealth = this.maxHealth;
		this.data = new PlayerData();
		this.data.win = false;
		this.data.name = "Squiddie";
		this.ability = "None";
		updateWeaponModel(0);
	}

	void FixedUpdate() {
		// dirty solution to players being pushed around by enemies
		transform.position = new Vector3(transform.position.x, 1.5f, transform.position.z);

		if (debug_On && hammerCooldown != -1 && --hammerCooldown < 0) {
			Debug.Log("Hammer available!");
			hammerCooldown = 0;
		}

		if (Time.time > nextAttackTime) {
			this.animator.SetInteger("Attack", 0);
			//ParticleMovement p = (ParticleMovement) GetComponentInChildren<ParticleMovement>();

			if (!this.animator.GetBool("Walking")) {
				foreach (Touch t in Input.touches) {


					if (t.phase == TouchPhase.Began) {
						this.initialTouch = t;
					} else if (t.phase == TouchPhase.Moved) {
						float deltaX = this.initialTouch.position.x - t.position.x;
						float deltaY = this.initialTouch.position.y - t.position.y;
						float distance = Mathf.Sqrt(Mathf.Pow(deltaX, 2) + Mathf.Pow(deltaY, 2));
						bool horizontalAttack = Mathf.Abs(deltaY / deltaX) < 1f;
						bool verticalAttack = Mathf.Abs(deltaY / deltaX) > 1f;

						if (distance > 100f) {
							if (horizontalAttack) {
								if (deltaX > 0)
									this.animator.SetInteger("Attack", 2);
								else
									this.animator.SetInteger("Attack", 1);
								this.attackType = 1;
							} else if (verticalAttack) {
								if (deltaY < 0)
									this.animator.SetInteger("Attack", 4);
								else
									this.animator.SetInteger("Attack", 3);
								this.attackType = 2;
							} else if (deltaX <= 0) {
								this.attackType = 4;
							} else if (deltaX > 0) {
								this.attackType = 8;
							}
							this.Attack();
						}
					} else if (t.phase == TouchPhase.Ended) {
						this.initialTouch = new Touch();
					}
				}
			}

			if (Input.GetKeyUp(KeyCode.Alpha1)) {
				this.attackType = 1;
				this.Attack();
				this.createPath();
				this.animator.SetInteger("Attack", 4);
			} else if (Input.GetKeyUp(KeyCode.Alpha2)) {
				this.attackType = 2;
				this.Attack();
				this.animator.SetInteger("Attack", 2);
			} else if (Input.GetKeyUp(KeyCode.Alpha3)) {
				this.attackType = 4;
				this.Attack();
			} else if (Input.GetKeyUp(KeyCode.Alpha4)) {
				this.attackType = 8;
				this.Attack();
			} else if (Input.GetKeyUp(KeyCode.Alpha5)) {          // for special abilities
				this.attackType = 1;
				this.AOE(30, damage, 1);
			}
		}
	}

	/// <summary>
	/// Actions that the player performs while in a state of idleness.
	/// </summary>
	/// <param name="joystick">Joystick controlling player movement.</param>
	public void Idle(CNAbstractController joystick) {
		this.animator.SetBool("Walking", false);
		this.StopCoroutine("RotateCoroutine");
	}

	/// <summary>
	/// Actions that the player performs while in a state of motion.
	/// </summary>
	/// <param name="input">Input movement vector.</param>
	/// <param name="joystick">Joystick controlling player movement.</param>
	public void Move(Vector3 input, CNAbstractController joystick) {
		this.animator.SetBool("Walking", true);
		Vector3 movement = new Vector3(input.x, 0f, input.y);
		movement = Camera.main.transform.TransformDirection(movement);
		movement.y = 0f;
		//		movement.Normalize();	// Allow movement sensitivity

		StopCoroutine("RotateCoroutine");
		StartCoroutine("RotateCoroutine", movement);
		this.GetComponent<CharacterController>().Move(Time.deltaTime * moveSpeed * movement);
	}

	/// <summary>
	/// Controls rotation of the player character based on an input vector.
	/// </summary>
	/// <returns>The coroutine.</returns>
	/// <param name="direction">Input vector for direction of rotation.</param>
	private IEnumerator RotateCoroutine(Vector3 direction) {
		if (direction == Vector3.zero) {
			yield break;
		}

		do {
			this.transform.rotation = Quaternion.Lerp(
				this.transform.rotation,
				Quaternion.LookRotation(direction),
				Time.deltaTime * this.rotateSpeed
			);
			yield return null;
		} while ((direction - this.transform.forward).sqrMagnitude > 0.2f);
	}

	public override bool Attack() {
		// Test if the weapon is hammer
		if (weaponButton.weapon == weaponButton.weaponList[0] && weaponButton.active) {
			if (weaponButton.wall != null) {
				Debug.Log("Destroy the wall!");
				weaponButton.wall.DestroyWall();
				weaponButton.wall = null;
				weaponButton.setCooldown ();
				// Add code here for cooldown reset
			}
		}
		bool hasAttacked = base.Attack();
		if (hasAttacked) {
			SoundController.PlaySound(GetComponent<AudioSource>(), attackSound[0]);
			if(weaponButton.weapon == weaponButton.weaponList[4] && weaponButton.active) {
				nextAttackTime = Time.time;
			}

			if (weaponButton.active) {
				weaponButton.setCooldown ();
			}
		}
		return hasAttacked;
	}

	public void createPath() {
		ParticleGenerator p = (ParticleGenerator) GetComponentInChildren<ParticleGenerator>();
		/*if (attackType == 1) {
			ParticleMovement slash = p.createPath (new Vector3(Screen.width / 8.0f, Screen.height / 2.0f));
			//Debug.Log (slash.transform.position);
			slash.move (new Vector3(Screen.width * 7 / 8.0f, Screen.height / 2.0f));
			//Debug.Log (slash.transform.position);

			Destroy (slash);
		}*/

	}

	public override bool TakeDamage(int enDamage, int enAttackType) {
		if (enAttackType == 15) {
			enDamage = enDamage;
		}

		this.currentHealth = this.currentHealth - enDamage;
		if (debug_On)
			Debug.Log("Taking Damage: -" + enDamage);
		if (this.currentHealth <= 0) {
			this.currentHealth = 0;
			StartCoroutine(waitBeforeDie ());
			return true;
		}
		return false;
	}

	public IEnumerator waitBeforeDie() {
		yield return new WaitForSeconds (1.5f);
		this.Die();
	}

	public override void Die() {
		if (debug_On)
			Debug.Log("I am dead.");
			
		SoundController.PlaySound(GetComponent<AudioSource>(), attackSound[1]);
		transform.position = spawn;
		this.currentHealth = this.maxHealth;

		weaponButton.removeWeapon ();
		
		updateWeaponModel(0);
		weapon = null;
		armor = null;
		boots = null;
		checkItemsForSet();

		removeWeapon();
	}

	public void setSpawn(Vector3 start) {
		spawn = start;
	}

	public void addHealth(int h) {
		if (this.currentHealth + h <= baseMaxHealth)
			this.currentHealth += h;
		else
			this.currentHealth = baseMaxHealth;
	}

	public void addSpeed(float s) {
		this.baseMoveSpeed += s;
		updateStats();
	}

	public void addMaxHealth(int mh) {
		baseMaxHealth += mh;
		currentHealth += mh;
		updateStats();
	}

	public void addDamage(int d) {
		baseDamage += d;
		updateStats();
	}

	public void addStartSword() {
	}

	public void addHammer() {
		this.hammerCooldown = 0;
		AttackDetector detector = (AttackDetector) GetComponentInChildren<AttackDetector>();
	}

	public void addSword() {
		AttackDetector detector = (AttackDetector) GetComponentInChildren<AttackDetector>();
		detector.transform.localPosition = new Vector3(0, .4f, 10);
		detector.transform.localScale = new Vector3(2.5f, 3.5f, 20);
	}

	public void addFoil() {
	}

	// 
	public void addAbility(string weaponType) {
		bool valid = true;

		switch(weaponType) {
			case "hammer":
				weaponList[0].SetActive (true);
				break;

			case "sword":
				weaponList[1].SetActive (true);
				break;

			case "foil":
				weaponList[2].SetActive (true);
				break;

			case "startsword":
				weaponList[3].SetActive (true);
				break;

			default:
				Debug.Log ("that wasn't a valid weapon, friend");
				valid = false;
				break;
		}

		if(valid) {
			ability = weaponType;
		}
	}

	// Removes the player's ability.
	public void removeAbility() {
		removeWeapon ();

		for(int i = 0; i < weaponList.Length; i++) {
			weaponList[i].SetActive (false);
		}

		ability = "";
	}
	
	public void toggleAbility() {
		if(ability == "") {
			return;
		}

		if(usingAbility) {
			removeWeapon ();
		}
		else {
			if(ability.Equals ("hammer")) {
				addHammer ();
			}

			else if(ability.Equals ("sword")) {
				addSword ();
			}

			else if(ability.Equals("foil")) {
				addFoil ();
			}

			else if(ability.Equals ("startsword")) {
				addStartSword ();
			}

		}

		usingAbility = !usingAbility;
	}


	public void AOE(float radius, int damage, int dmgType) {
		Collider[] monsterColliders = Physics.OverlapSphere(this.transform.position, radius);
		int i = 0;

		while (i < monsterColliders.Length) {
			if (monsterColliders[i].tag == "Monster") {
				EnemyCharacter target = monsterColliders[i].GetComponent<EnemyCharacter>();
				target.TakeDamage(damage, dmgType);
			}
			i++;
		}
	}

	public void removeWeapon() {
		this.hammerCooldown = -1;
//		AttackDetector detector = (AttackDetector) GetComponentInChildren<AttackDetector>();
//		detector.transform.localPosition = new Vector3(0, 0.4f, 2);
//		detector.transform.localScale = new Vector3(2.5f, 3.5f, 3.5f);
	}

	/*public void equipItem(Item newItem) {
		switch (newItem.itemType) {
			case 0:
				this.weapon = newItem;
				break;
			case 1:
				this.armor = newItem;
				break;
			case 2:
				this.boots = newItem;
				break;
		}
		updateBonusStats();
		checkItemsForSet();
	}*/
	
	public void updateWeaponModel(int modelID)
	{
		if (debug_On)
			Debug.Log ("updating weapon model " + modelID);
		
		if (modelID < 0)
			return;
		
		for (int i = 0; i < WeaponModelList.Length; i++)
		{
			WeaponModelList[i].SetActive(false);
			if (i == modelID)
				WeaponModelList[i].SetActive(true);
		}
	}

	public void updateStats() {
		this.damage = this.baseDamage + this.bonusDamage;
		this.maxHealth = this.baseMaxHealth + this.bonusMaxHealth;
		this.moveSpeed = this.baseMoveSpeed + this.bonusMoveSpeed;
	}

	public void updateBonusStats() {
		bonusDamage = 0;
		bonusMaxHealth = 0;
		bonusMoveSpeed = 0;

		if (weapon) {
			bonusDamage += weapon.bonusDamage;
			bonusMaxHealth += weapon.bonusMaxHealth;
			bonusMoveSpeed += weapon.bonusMoveSpeed;
		}
		if (armor) {
			bonusDamage += armor.bonusDamage;
			bonusMaxHealth += armor.bonusMaxHealth;
			bonusMoveSpeed += armor.bonusMoveSpeed;
		}
		if (boots) {
			bonusDamage += boots.bonusDamage;
			bonusMaxHealth += boots.bonusMaxHealth;
			bonusMoveSpeed += boots.bonusMoveSpeed;
		}

		updateStats();
	}

	public void checkItemsForSet() {
		if (weapon && armor) {
			if (weapon.setVal == armor.setVal) {
				abilityType = weapon.setVal;
				abilityLevel = 1;
			}
		}
		if (weapon && boots) {
			if (weapon.setVal == boots.setVal) {
				abilityType = weapon.setVal;
				abilityLevel = 1;
			}
		}
		if (armor && boots) {
			if (armor.setVal == boots.setVal) {
				abilityType = armor.setVal;
				abilityLevel = 1;
			}
		}

		if (weapon && armor && boots) {
			if (weapon.setVal == armor.setVal && weapon.setVal == boots.setVal) {
				abilityType = weapon.setVal;
				abilityLevel = 2;
			}
		}
	}

	public int calculateScore() {
		return kills * 1000 + bonusDamage * 100 + bonusMaxHealth * 50 + (int) bonusMoveSpeed * 25;
	}

	public void setWall(EditWalls wall) {
		this.wall = wall;
	}
}

