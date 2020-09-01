using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlaneControllerForce : MonoBehaviour {
    Rigidbody rb;
    bool isPaused = false;
    bool looped = false;
    bool crashed = false;
    bool success = false;
    float lastX;
    int lastRotation;
    float currentTimer = 0;
    [SerializeField]
    float nextLevelTimer;
    [SerializeField]
    string nextLevel;
    [SerializeField]
    GameObject frontWheels;
    [SerializeField]
    GameObject backWheels;
    [SerializeField]
    Slider fuelMeter;
    [SerializeField]
    ParticleSystem smokeParticle;
    [Header("Base Stats")]
    [SerializeField]
    float baseForce = 5f;
    [SerializeField]
    float moveForce = 30f;
    [SerializeField]
    float maxVel = 50f;
    [SerializeField]
    float rotationSpeed = 15f;
    [SerializeField]
    float passiveRoation = 3f;
    [SerializeField]
    float tankMax = 100f;
    [SerializeField]
    float tankCurrent = 100f;
    [SerializeField]
    float tankDepletionRate = 1f;

    [Header("Modifiers")]
    [SerializeField]
    float rotationModifier = 0f;
    [SerializeField]
    float speedModifier = 0f;
    [SerializeField]
    float constantRotationModifier = 0f;
    [SerializeField]
    float rotationDifficulty;

    // Start is called before the first frame update
    void Start() {
        rb = GetComponent<Rigidbody>();
        fuelMeter.maxValue = tankMax;
        fuelMeter.value = tankCurrent;
        lastX = transform.rotation.x;
    }
    void Update() {
        // Levels, pause and sounds
        if (Input.GetKeyDown(KeyCode.Escape)) isPaused = !isPaused;
        if (isPaused) {
            Time.timeScale = 0f;
        } else {
            Time.timeScale = 1f;
        }
        if (Input.GetKeyDown(KeyCode.W) && tankCurrent > 0 && !success && !crashed) FindObjectOfType<AudioManager>().Play("Acc LightAircraft");
        if ((Input.GetKeyUp(KeyCode.W) && tankCurrent > 0) || tankCurrent <= 0) FindObjectOfType<AudioManager>().Stop("Acc LightAircraft");
        if (success) {
            currentTimer += Time.deltaTime;
            if (currentTimer >= nextLevelTimer) {
                FindObjectOfType<AudioManager>().Stop("Theme");
                SceneManager.LoadScene(nextLevel);
            }
        } else if (crashed) {
            currentTimer += Time.deltaTime;
            if (currentTimer >= nextLevelTimer) {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }
    void FixedUpdate() {
        // Controlling the player
        if (crashed) return;
        fuelMeter.value = tankCurrent;
        // Power up
        if (Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S) && tankCurrent > 0) {
            rb.velocity = rb.transform.forward * moveForce + new Vector3(0f, 0f, speedModifier);
            if (tankCurrent > 5) {
                tankCurrent -= tankDepletionRate*Time.deltaTime;
            } else {
                tankCurrent -= tankDepletionRate/2 * Time.deltaTime;
            }
            // Debug.Log(tankCurrent);
            if (looped || Input.GetKey(KeyCode.B)) {
                // Debug.Log("Burst!");
                smokeParticle.Play();
                looped = false;
            }
            lastX = transform.rotation.x;
        }
        // Power down
        else if (Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W)) {
            if (rb.velocity.z > 0) rb.velocity = -rb.transform.forward;
        }
        else {
            rb.AddForce(baseForce * transform.forward);
            // Debug.Log(-Mathf.Round(transform.rotation.x * 10f) * 0.1f + " lastX: " + Mathf.Round(lastX * 10f) * 0.1f);
            // Still not triggering on the exact loop but triggers sporadically enough to make it a fun thing to see
            if (-Mathf.Round(transform.rotation.x * 10f) * 0.1f == Mathf.Round(lastX * 10f) * 0.1f && Mathf.Round(lastX * 10f) * 0.1f != 0) {
                // Debug.Log("LOOP!");
                looped = true;
            }
        }

        // Throttle Up
        if (Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D)) {
            transform.Rotate(
                (-rotationSpeed * Time.deltaTime) - rotationModifier + rotationDifficulty - constantRotationModifier,
                0f, 0f);
            lastRotation = -1;
        }
        // Throttle Down
        else if (Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A)) {
            transform.Rotate(
                (rotationSpeed * Time.deltaTime) + rotationModifier - rotationDifficulty + constantRotationModifier,
                0f, 0f);
            lastRotation = 1;
        } else {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S)) {
                transform.Rotate(constantRotationModifier, 0f, 0f);
            } else {
                transform.Rotate(constantRotationModifier + (lastRotation * passiveRoation), 0f, 0f);
            }
        }
        // Debug.Log("Magnitude: " + rb.velocity.magnitude + ", Velocity Z" + rb.velocity.z);
    }
    private void OnCollisionEnter(Collision collision) {
        if (crashed) return;
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Ground")) {
            // Debug.Log("Failure Ground");
            FindObjectOfType<AudioManager>().Play("Metal Crashes");
            FindObjectOfType<AudioManager>().Stop("Idle LightAircraft");
            FindObjectOfType<AudioManager>().Stop("Acc LightAircraft");
            crashed = true;
            return;
        }
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Landing")) {
            if (transform.gameObject.name == collision.contacts[0].thisCollider.gameObject.name) {
                // Debug.Log("Failure");
                FindObjectOfType<AudioManager>().Play("Metal Crashes");
                FindObjectOfType<AudioManager>().Stop("Idle LightAircraft");
                FindObjectOfType<AudioManager>().Stop("Acc LightAircraft");
                crashed = true;
            }
            else {
                crashed = true;
                success = true;
                rb.constraints = RigidbodyConstraints.FreezeRotationX;
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.layer == LayerMask.NameToLayer("Fuel")) {
            if (tankCurrent + other.transform.gameObject.GetComponent<FuelTank>().fuelAmount >= tankMax) {
                FindObjectOfType<AudioManager>().Play("Liquid Moving");
                tankCurrent = tankMax;
            } else {
                tankCurrent += other.transform.gameObject.GetComponent<FuelTank>().fuelAmount;
                FindObjectOfType<AudioManager>().Play("Liquid Moving");
            }
            Destroy(other.transform.gameObject);
        }
    }
}