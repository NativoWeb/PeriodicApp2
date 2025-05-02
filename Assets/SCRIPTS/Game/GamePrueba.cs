using UnityEngine;

public class GamePrueba : MonoBehaviour
{
    public float FuerzaSalto;
    private Rigidbody2D rb;
    public Transform refpie;
    private bool enSuelo = false;
    public float velX = 5f;
    Animator anim;
    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float movX;
        movX = Input.GetAxis("Horizontal");
        anim.SetFloat("absMovX", Mathf.Abs(movX));
        rb.linearVelocity = new Vector2(velX * movX, rb.linearVelocity.y);

        enSuelo = Physics2D.OverlapCircle(refpie.position, 1f, 1 << 8);
        anim.SetBool("enPiso", enSuelo);

        if (Input.GetButtonDown("Jump") && enSuelo)
        {
            rb.AddForce(new Vector2(0, FuerzaSalto), ForceMode2D.Impulse);
        }

        if (movX < 0)
        {
            transform.localScale = new Vector3(-1f, 1f, 1f);
        }

        if (movX > 0)
        {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }
}
