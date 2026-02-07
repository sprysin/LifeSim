# LifeSim: Concept and Goals

## Overview
LifeSim is a unique hybrid application that combines the mechanics of a Life Simulator game with an advanced AI Chat Bot. The core experience revolves around living with an AI roommate who is not just a script, but a dynamic, thinking entity. The AI will form memories, experience changing emotions, and perform actions within the game world completely independent of user input. The world itself is a top down 16x16 pixel tile based world. The player as well as NPCs are two tiles tall. 

## Core Features

### 1. The AI Roommate
- **Autonomy**: The roommate will form memories, experience changing emotions, and perform actions within the game world completely independent of user input.
- **Dynamic Personality**: Their mood and behavior will shift dynamically based on their internal state and external interactions.
- **Gemini API**: The intelligence of the roommate is powered by the Gemini API, allowing for rich, context-aware responses and behaviors.

### 2. Interaction System
- **Chat Box**: The primary interface for communication. The user can engage in two types of interaction:
    - **Human Dialogue**: Direct conversation with the roommate.
    - **Soft RP Actions**: Describing actions (e.g., *hands a cup of coffee*) to roleplay scenarios.
- **Mood Influence**: Every interaction (text or action) impacts the roommate's mood, creating a reactive relationship.

### 3. Environmental Interaction
- **Functional Objects**: Household objects will not just be decorative. They will gain functionality allowing for shared interactions.
- **Terminal System**: The in-game terminal will serve as a deeper interface for interacting with the system and the roommate.

## Project Goals
- To create a "living" AI companion that feels present and responsive.
- To seamlessly blend traditional game loops (actions, objects) with the open-ended nature of Large Language Models.
- To deliver an immersive "soft roleplay" experience where the player builds a relationship with their AI roommate.

## Phase 2: Implementation Roadmap

### Step 1: Gemini "Brain" Integration
*   [X] **API Client**: Create a C# service (`GeminiService.cs`) to handle interactions with the Gemini API.
*   [X] **Prompt Engineering**: Design the "System Prompt" that defines the roommate's persona, rules for "Roleplay," and how they should format their internal thoughts vs. external dialogue.

### Step 2: Memory & State System
*   [ ] **Memory Manager**: Implement a system to store short-term (active conversation) and long-term (summarized events) memories.
*   [ ] **State Vector**: Define the variables that make up the "Mood" and "State" (e.g., Energy, Happiness, Trust). These need to be passed to Gemini so it knows how to feel.

### Step 3: The Autonomous Loop
*   [ ] **Simulation Ticks**: Create a game loop that triggers every few seconds/minutes to let the AI "decide" a move (Move to Kitchen, Sleep, Read) without player input.
*   [ ] **Action Parsing**: Teach the AI to output commands like `[ACTION: MOVE_KITCHEN]` which the game engine parses and executes.

### Step 4: Chat Interface Overhaul
*   [X] **Dynamic History**: Format the chat log so the visual history distinguishes between *Actions* and **Speech** (Bold/Color).

### Step 5: Interactive Environment
*   [ ] **Smart Objects**: Update `TileSystem` or `Interactable` classes so objects can report their status to the AI (e.g., "The TV is ON").
*   [ ] **Shared State**: Ensure both Player and AI can affect these objects, creating conflict or cooperation opportunities.
