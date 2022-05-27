/* L-System Generator Project for the course
 * of Artificial Intelligence for Videogames.
 * Manuel Pagliuca, University of Milan, A.Y. 2021/2022 */

using UnityEngine;

public class Parser : MonoBehaviour
{
    private string derivedStr = string.Empty;
    LSystemGenerator generator;
    bool isTree = true;

    public void Parse()
    {
        foreach (char c in derivedStr)
        {
            switch (c)
            {
                case 'X':
                    break;

                case 'F':
                    generator.GenerateBranch();
                    if (!isTree) isTree = true;
                    break;

                case 'R':
                    generator.GenerateRoot();
                    if (isTree) isTree = false;
                    break;

                case 'L':
                    generator.GenerateLeaf();
                    break;

                case '*':
                    generator.RotateRnd();
                    break;

                case '[':
                    generator.Push();
                    break;

                case ']':
                    generator.Pop();
                    break;

                default:
                    throw new System.InvalidOperationException("Invalid symbol.");
            }
        }
    }

    public void SetLSGenerator(LSystemGenerator lsys)
    {
        generator = lsys;
    }

    public void SetString(string derivedStr)
    {
        this.derivedStr = derivedStr;
    }
}