﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UI_Handler : MonoBehaviour{

    [SerializeField] TextMeshProUGUI energyText;
    [SerializeField] TextMeshProUGUI questionText;

    bool answered, canLoadQuestion;
    int answer, givenAnswer, energyValue;

    void Start()    {
        // Reset all paramaters
        energyText.SetText("Energy: 0"); // Start with zero energy   
        answered = canLoadQuestion = true; // Get new question
        givenAnswer = energyValue = 0;
        answer = 5;
    }


    void Update()    {
        if (answered && canLoadQuestion) {

            if (givenAnswer == answer) {
                energyValue++;  // Increase Energy
                energyText.SetText("Energy: " + energyValue.ToString());  // Update GUI
                givenAnswer = 0;    // Reset givenAnswer
                FindObjectOfType<GameHandler>().SetEnergyValue(energyValue);   // Update energy value on GM 
            }

            //Set new answer
            answer = Random.Range(1, 4);
            questionText.SetText("Answer is " + answer.ToString());

            answered = false; // Reset answered
        }

        energyText.SetText("Energy: " + energyValue);   // Update Energy Value
    }

    // Getting answer
    public void SetAnswer(int givenAnswer) {
        answered = true;
        this.givenAnswer = givenAnswer;
    }

    public void SetEnergyValue(int energyValue) {
        this.energyValue = energyValue;
    }
    public void SetLoadQuestion(bool canLoadQuestion) {
        this.canLoadQuestion = canLoadQuestion;
    }

}