using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class TriggerTrampaLancha : MonoBehaviour
{
    public TrampaLanchaArranque trampa;

    void Reset()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 10f);
        col.offset = new Vector2(0f, 4f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Grace.En(other) == null)
            return;

        if (trampa != null)
            trampa.ActivarDesdeTrigger();
    }
}
