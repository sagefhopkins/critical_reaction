using UnityEngine;
using UnityEngine.UI;

public class LabNotebook : MonoBehaviour
{
    public Text notebookText;

    private void Start()
    {
        UpdateNotebook();
    }

    public void UpdateNotebook()
    {
        var skills = CampaignManager.Instance.progress.completedSkills;
        notebookText.text = "Lab Notebook \n\n";

        foreach (string skill in skills)
        {
            notebookText.text += "✔" + skill + "\n";
        }
    }
}
