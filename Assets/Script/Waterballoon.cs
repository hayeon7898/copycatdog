using UnityEngine;

public class Waterballoon : MonoBehaviour
{
    public int Power; // ��ǳ�� ���� ����

    private void Start()// ��ǳ�� ����
    {
        StartCoroutine(ExplodeAfterDelay(5f));
    }

    private System.Collections.IEnumerator ExplodeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Explode();
    }

    private void Explode()
    {
        Debug.Log("��ǳ���� �������ϴ�!"); // �� ���� ��, ������ �������� ����
    }
}

