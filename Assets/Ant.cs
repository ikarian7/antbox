using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Ant : MonoBehaviour
{
    Rigidbody2D rb;
    List<Ant> antsInRange;

    [SerializeField] float pushForce = 100f;
    [SerializeField] float boxForce = 250f;

    [SerializeField] float lightningTime = .2f;
    [SerializeField] int lightningParticles = 25;
    [SerializeField] float feedbackTime = 1.5f;
    [SerializeField] int feedbackParticles = 10;


    [SerializeField] GameObject highlight;

    Rect boxRect;

    ParticleSystem lightningPS, feedbackPS;

    static List<Ant> antsBeingLit, antsLit;
    static bool lightningActive { get { return antsBeingLit.Count > 0; } }

    Ant litFrom, feedbackTo;
    bool passedOn;
    float lightDuration, feedbackDuration;
    [SerializeField] public bool BeingLit { get { return lightDuration > 0f; } }
    [SerializeField] public bool IsLit { get { return highlight.activeSelf; } }
    static Stack<Ant> stackAnts = new Stack<Ant>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        antsInRange = new List<Ant>();

        if(antsLit == null) {
            antsLit = new List<Ant>();
            antsBeingLit = new List<Ant>();
        }

        BoxCollider2D box = GameObject.FindGameObjectWithTag("Box").GetComponent<BoxCollider2D>();
        Vector2 size = box.size * box.transform.lossyScale;
        boxRect = new Rect(new Vector2(box.transform.position.x - size.x / 2f, box.transform.position.y - size.y / 2f) + box.offset, size);

        lightningPS = GameObject.FindGameObjectWithTag("Lightning").GetComponent<ParticleSystem>();
        feedbackPS = GameObject.FindGameObjectWithTag("Feedback").GetComponent<ParticleSystem>();

        // A little randomness so every run will be different
        rb.velocity = UnityEngine.Random.insideUnitCircle;
    }
	
	void FixedUpdate()
    {
        // Push Ants away from the sides of the box
        Vector3 pos = transform.position;
        if(pos.x < boxRect.xMin) rb.AddForce(new Vector2(boxForce * (boxRect.xMin - pos.x), 0f));
        else if(pos.x > boxRect.xMax) rb.AddForce(new Vector2(boxForce * (boxRect.xMax - pos.x), 0f));
        if(pos.y < boxRect.yMin) rb.AddForce(new Vector2(0f, boxForce * (boxRect.yMin - pos.y)));
        else if(pos.y > boxRect.yMax) rb.AddForce(new Vector2(0f, boxForce * (boxRect.yMax - pos.y)));

        // Push Ants apart
        foreach(Ant inRange in antsInRange) {
            Vector3 pushDifference = transform.position - inRange.transform.position;
            rb.AddForce(pushForce * pushDifference.normalized / pushDifference.sqrMagnitude);
        }

        // Lightning transition
        if(litFrom != null) {
            if(lightDuration > 0f && !litFrom.BeingLit) {
                lightDuration -= Time.fixedDeltaTime;
                if(lightDuration <= 0f) {
                    lightDuration = 0f;
                    LightUp();
                    antsBeingLit.Remove(this);
                    antsLit.Add(this);

                    if(!passedOn) {
                        Debug.Log("Finsihed?");
                        DoFeedback();
                    }
                } else {
                    if(UnityEngine.Random.value < 100f * Time.fixedDeltaTime) {
                        lightningPS.transform.position = Vector3.Lerp(transform.position, litFrom.transform.position, lightDuration / lightningTime);
                        lightningPS.Emit(1);
                    }
                }
            }
        }

        // Feedback effect
        if(feedbackTo != null) {
            if(feedbackDuration > 0f) {
                for(int i = 0; i < feedbackParticles; i++) {
                    feedbackDuration -= Time.fixedDeltaTime / (float)feedbackParticles;
                    if(feedbackDuration <= 0f) {
                        feedbackDuration = 0f;
                        break;
                    } else {
                        feedbackPS.transform.position = Vector3.Lerp(feedbackTo.transform.position, transform.position, feedbackDuration / feedbackTime);
                        feedbackPS.Emit(2);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Called by AntBox when an Ant is clicked
    /// </summary>
    public void Clicked()
    {
        if(!lightningActive && !highlight.activeSelf) {
            highlight.SetActive(true);

            ChainLightning();
        }
    }

    /// <summary>
    /// Find the closest Ant that is in range and not lit up yet
    /// </summary>
    /// <returns>Closest Ant or null</returns>
    Ant FindClosestUnlitAnt()
    {
        float bestDistance = float.PositiveInfinity;
        Ant closest = null;

        foreach(Ant inRange in antsInRange) {
            if(!inRange.IsLit && !inRange.BeingLit) {
                float distance = Vector3.Distance(inRange.transform.position, transform.position);
                if(distance < bestDistance) {
                    bestDistance = distance;
                    closest = inRange;
                }
            }
        }

        return closest;
    }

    /// <summary>
    /// Cast chain lightning from an ant
    /// </summary>
    public void ChainLightning()
    {
        Ant closest = null;

        do
        {
            Ant previous;

            if (closest == null)
            {
                closest = this.FindClosestUnlitAnt();
                previous = this;
            }
            else
            {
                previous = closest;
                closest = closest.FindClosestUnlitAnt();
            }

            if (closest != null)
            {
                closest.LightFrom(previous);
                passedOn = true;
                stackAnts.Push(previous);
            }

        } while (closest != null);

    }

    /// <summary>
    /// Light up an Ant from another
    /// </summary>
    /// <param name="source">Ant the lightning originates from</param>
    public void LightFrom(Ant source)
    {
        if(IsLit) throw new InvalidOperationException(gameObject.name + " being lit, but is already lit!");

        litFrom = source;
        lightDuration = lightningTime;

        antsBeingLit.Add(this);
    }

    /// <summary>
    /// End of the lighting up sequence
    /// </summary>
    void LightUp()
    {
        for(int i = 0; i < lightningParticles; i++) {
            lightningPS.transform.position = Vector3.Lerp(transform.position, litFrom.transform.position, i / (float)lightningParticles);
            lightningPS.Emit(1);
        }

        highlight.SetActive(true);
    }

    /// <summary>
    /// Feedback to source Ant
    /// </summary>
    public void DoFeedback()
    {
        Debug.Log("StackAnts dofeedback count: " + stackAnts.Count);
        // Get last ant from stack and call FeedbackTo on it
        while (stackAnts.Count > 0)
        {
            Ant poppedAnt = stackAnts.Pop();
            Debug.Log(poppedAnt.name);
            FeedbackTo(poppedAnt);
        }
    }

    /// <summary>
    /// Play back feedback animation
    /// </summary>
    /// <param name="to">Target ant</param>
    public void FeedbackTo(Ant to)
    {
        feedbackTo = to;
        feedbackDuration = feedbackTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Update list of Ants in range
        Ant ant = other.GetComponent<Ant>();
        if(ant != null) antsInRange.Add(ant);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // Update list of Ants in range
        Ant ant = other.GetComponent<Ant>();
        if(ant != null) antsInRange.Remove(ant);
    }

    void OnDrawGizmos()
    {
        if(antsInRange != null) {
            Gizmos.color = Color.red;
            Vector3 pos = transform.position;

            if(pos.x < boxRect.xMin) {
                Gizmos.DrawLine(transform.position, transform.position + new Vector3(boxForce * (boxRect.xMin - pos.x), 0f));
                Gizmos.DrawWireSphere(new Vector3(boxRect.xMin, pos.y, pos.z), 1f);
            } else if(pos.x > boxRect.xMax) {
                Gizmos.DrawLine(transform.position, transform.position + new Vector3(boxForce * (boxRect.xMax - pos.x), 0f));
                Gizmos.DrawWireSphere(new Vector3(boxRect.xMax, pos.y, pos.z), 1f);
            }
            if(pos.y < boxRect.yMin) {
                Gizmos.DrawLine(transform.position, transform.position + new Vector3(0f, boxForce * (boxRect.yMin - pos.y)));
                Gizmos.DrawWireSphere(new Vector3(pos.x, boxRect.yMin, pos.z), 1f);
            } else if(pos.y > boxRect.yMax) {
                Gizmos.DrawLine(transform.position, transform.position + new Vector3(0f, boxForce * (boxRect.yMax - pos.y)));
                Gizmos.DrawWireSphere(new Vector3(pos.x, boxRect.yMax, pos.z), 1f);
            }

            Gizmos.color = Color.magenta;
            foreach(Ant inRange in antsInRange) {
                Gizmos.DrawLine(transform.position, inRange.transform.position);
            }
        }
    }
}
