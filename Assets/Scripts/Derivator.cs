/* L-System Generator Project for the course
 * of Artificial Intelligence for Videogames.
 * Manuel Pagliuca, University of Milan, A.Y. 2021/2022 */

using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Derivator: MonoBehaviour
{
    private Dictionary<char, string> rules;
    private string derivedString = string.Empty;

    public void SetAxiomAndRules(char axiom, Dictionary<char, string> t_rules)
    {
        derivedString = string.Empty;
        derivedString += axiom;
        rules = t_rules;
    }

    /* Generate the Well-Formed Formula (WFF), which is synctactic correct string of symbols
     * from the rules of the given grammar. This string will be the representation of the tree. */
    public string Derive(int iterations)
    {
        StringBuilder buffer = new StringBuilder();

        for (int i = 0; i < iterations; i++)
        {
            foreach (char c in derivedString)
                buffer.Append(rules.ContainsKey(c) ? rules[c] : c.ToString());

            derivedString = buffer.ToString();
            buffer = new StringBuilder();
        }

        return derivedString;
    }
}