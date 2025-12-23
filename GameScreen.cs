using Godot;
using System;
using System.Collections.Generic;

public partial class GameScreen : Node2D
{
	// ୨ৎ SCENE REFERENCES ୨ৎ
	[Export] public TextureRect KitchenPhoto;
	[Export] public TextureRect ClosetPhoto;
	[Export] public Label KitchenTitle;
	[Export] public Label ClosetTitle;
	[Export] public Sprite2D Character;
	[Export] public VideoStreamPlayer RecipeVideoPlayer;
	[Export] public Control RecipePopup;

	// ୨ৎ BURGER CAKE GAME REFERENCES ୨ৎ
	[Export] public Control BurgerGameContainer; 
	[Export] public Sprite2D BurgerPlate;           
	[Export] public Label InstructionLabel;

	// ୨ৎ SPECIAL BUTTON VISIBILITY ୨ৎ
	[Export] public TextureButton VanillaFrostingButton;
	[Export] public TextureButton ChocolateFrostingButton;
	[Export] public TextureButton PlateButton;
	[Export] public TextureButton TopBunButton;

	[Export] public int ShowVanillaStep = 22; 
	[Export] public int ShowChocolateStep = 26;
	[Export] public int SwapPlateToBunStep = 27;
	[Export] public int HideTopBunStep = 33;
	
	// ୨ৎ AUDIO ୨ৎ
	[Export] public AudioStreamPlayer MusicPlayer;
	[Export] public AudioStreamPlayer ClickSound;
	[Export] public AudioStreamPlayer LockedSound; 
	
	[Export] public float VideoVolumeDb = 10.0f; // ♫ volume boost ♫

	// ୨ৎ VIDEO CONTROLS ୨ৎ
	[Export] public TextureButton RestartButton;
	[Export] public TextureButton RewindButton;
	[Export] public TextureButton SkipButton;
	[Export] public TextureButton CloseVideoButton;
	[Export] public TextureButton PauseButton;
	
	[Export] public Texture2D PlayIcon;
	[Export] public Texture2D PauseIcon;

	// ୨ৎ NAVIGATION BUTTONS ୨ৎ
	[Export] public TextureButton ClosetButton;
	[Export] public TextureButton NextOutfitButton;
	[Export] public TextureButton BackToKitchenButton;
	[Export] public TextureButton RecipeBookButton;
	[Export] public TextureButton BurgerRecipeButton; 
	[Export] public TextureButton CloseRecipeButton;

	// ୨ৎ RECIPE SYSTEM ୨ৎ
	[Export] public TextureButton[] RecipeButtons;
	[Export] public VideoStream[] RecipeStreams;
	[Export] public int VideosReadyCount = 1; 

	// ୨ৎ BURGER GAME LOGIC ୨ৎ
	[Export] public TextureButton[] IngredientButtons;
	[Export] public Texture2D[] BurgerStages; 
	[Export] public string[] StepInstructions;
	
	private int _currentBurgerStep = 0;

	// ୨ৎ POSITIONS & CROP ୨ৎ
	[Export] public Vector2 KitchenPosition;
	[Export] public Vector2 ClosetPosition;
	[Export] public Rect2 KitchenCropRegion;

	// ୨ৎ WARDROBE ୨ৎ
	[Export] public Godot.Collections.Array<Texture2D> Outfits;
	private int _currentOutfitIndex = 0;

	// ୨ৎ SCALING VARIABLES ୨ৎ
	private Vector2 _baseScale;
	private float _targetHeightPixels;

	// ♡⸝⸝ SETUP ♡⸝⸝
	public override void _Ready()
	{
		// 1. Initial Visibility
		if (KitchenPhoto != null) KitchenPhoto.Visible = true;
		if (ClosetPhoto != null) ClosetPhoto.Visible = false;

		
		if (RecipePopup != null) RecipePopup.Visible = false;
		if (RecipeVideoPlayer != null) RecipeVideoPlayer.Visible = false;
		
		
		if (BurgerGameContainer != null) 
		{
			BurgerGameContainer.Visible = true;
			_currentBurgerStep = 0; 
			UpdateBurgerVisuals();  
		}
 
		// 2. Character Setup & Size Capture
		if (Character != null)
		{
			_baseScale = Character.Scale; 
			Character.RegionEnabled = true;
			
			if (KitchenPosition != Vector2.Zero) Character.Position = KitchenPosition;
			if (KitchenCropRegion.HasArea()) Character.RegionRect = KitchenCropRegion;

			if (Character.RegionEnabled)
				_targetHeightPixels = Character.RegionRect.Size.Y;
			else if (Character.Texture != null)
				_targetHeightPixels = Character.Texture.GetHeight();
		}

		// 3. APPLY BUTTON HOVER EFFECTS (Navigation)
		SetupButtonHover(ClosetButton);
		SetupButtonHover(BackToKitchenButton);
		SetupButtonHover(RecipeBookButton);
		SetupButtonHover(CloseRecipeButton);
		SetupButtonHover(NextOutfitButton);
		SetupButtonHover(CloseVideoButton);
		SetupButtonHover(PauseButton);
		SetupButtonHover(RestartButton);
		SetupButtonHover(RewindButton);
		SetupButtonHover(SkipButton);
		SetupButtonHover(BurgerRecipeButton);

		// 4. SETUP RECIPE BOOK
		SetupRecipeButtons();
		
		// 5. SETUP INGREDIENTS
		SetupIngredientButtons();
		
		// 6. Connect the Next Outfit Button
		if (NextOutfitButton != null) 
	{
			SetupButtonHover(NextOutfitButton);
   			 if (!NextOutfitButton.IsConnected("pressed", new Callable(this, nameof(OnNextOutfitPressed))))
  		{
		NextOutfitButton.Pressed += OnNextOutfitPressed;
		}
	}
}

	// ♡⸝⸝ BURGER CAKE VIDEO ♡⸝⸝
	public void StartBurgerCakeRecipe()
	{
		GD.Print("Burger Cake Selected!");
		PlayClick();

		if (RecipePopup != null) RecipePopup.Visible = false;
		if (MusicPlayer != null) MusicPlayer.StreamPaused = true;
		 // Volume Boost 
			RecipeVideoPlayer.VolumeDb = VideoVolumeDb;
		if (RecipeVideoPlayer != null)
		{
			RecipeVideoPlayer.Visible = true;
			if (RecipeStreams != null && RecipeStreams.Length > 0 && RecipeStreams[0] != null)
			{
				RecipeVideoPlayer.Stream = RecipeStreams[0];
			}
			
			RecipeVideoPlayer.Play();
			RecipeVideoPlayer.Paused = false;
			
			if (PauseButton != null && PauseIcon != null) 
				PauseButton.TextureNormal = PauseIcon;
		}
	}

	// ♡⸝⸝ BURGER GAME LOGIC ♡⸝⸝
	private void SetupIngredientButtons()
	{
		if (IngredientButtons == null) return;

		var connectedButtons = new HashSet<TextureButton>();

		for (int i = 0; i < IngredientButtons.Length; i++)
		{
			TextureButton btn = IngredientButtons[i];
			if (btn == null) continue;

			if (!connectedButtons.Contains(btn))
			{
				SetupButtonHover(btn); 
				
				// Direct connection with lambda sending the pressed button
				btn.Pressed += () => OnIngredientPressed(btn);
				connectedButtons.Add(btn);
			}
		}
	}

	public void StartBurgerGame()
	{
		GD.Print("Starting Burger Game...");
		PlayClick();

		_currentBurgerStep = 0;
		UpdateBurgerVisuals();

		if (RecipePopup != null) RecipePopup.Visible = false;
		if (BurgerGameContainer != null) BurgerGameContainer.Visible = true;
	}

	private void OnIngredientPressed(TextureButton pressedBtn)
	{
		if (IngredientButtons == null || _currentBurgerStep >= IngredientButtons.Length) return;

		TextureButton requiredBtn = IngredientButtons[_currentBurgerStep];

		if (pressedBtn == requiredBtn)
		{
			if (ClickSound != null) ClickSound.Play();
			
			_currentBurgerStep++;
			UpdateBurgerVisuals();
			
			if (BurgerStages != null && _currentBurgerStep >= BurgerStages.Length)
			{
				if (InstructionLabel != null) InstructionLabel.Text = "Delicious!";
			}
		}
		else
		{
			GD.Print("Wrong Ingredient!");
		}
	}

	private void UpdateBurgerVisuals()
	{

		if (BurgerStages == null || BurgerStages.Length == 0) return;

		int safeIndex = Math.Clamp(_currentBurgerStep, 0, BurgerStages.Length - 1);
		Texture2D currentImg = BurgerStages[safeIndex];

		// Sprite2D
		if (BurgerPlate != null)
		{
			BurgerPlate.Visible = true;
			BurgerPlate.Texture = currentImg;
		}

		if (InstructionLabel != null && StepInstructions != null)
		{
			if (_currentBurgerStep < StepInstructions.Length)
				InstructionLabel.Text = StepInstructions[_currentBurgerStep];
			else
				InstructionLabel.Text = "Done!";
		}

		// ♡⸝⸝ SPECIAL BUTTON VISIBILITY LOGIC ♡⸝⸝
		
		// 1. Vanilla Frosting
		if (VanillaFrostingButton != null)
		{
			VanillaFrostingButton.Visible = (_currentBurgerStep >= ShowVanillaStep);
		}

		// 2. Chocolate Frosting
		if (ChocolateFrostingButton != null)
		{
			ChocolateFrostingButton.Visible = (_currentBurgerStep >= ShowChocolateStep);
		}

		// 3. Plate vs Top Bun Swap
		if (PlateButton != null && TopBunButton != null)
		{
			bool showTopBun = (_currentBurgerStep >= SwapPlateToBunStep);
			PlateButton.Visible = !showTopBun;  
			TopBunButton.Visible = showTopBun && (_currentBurgerStep < HideTopBunStep);
		}
	}
	
	// ♡⸝⸝ RECIPE SYSTEM LOGIC ♡⸝⸝
	private void SetupRecipeButtons()
	{
		if (RecipeButtons == null) return;

		for (int i = 0; i < RecipeButtons.Length; i++)
		{
			TextureButton btn = RecipeButtons[i];
			if (btn == null) continue;

			SetupButtonHover(btn);
			btn.Disabled = false; 

			bool isReady = (i < VideosReadyCount); 

			if (isReady)
			{
				int index = i; 
				if (!btn.IsConnected("pressed", new Callable(this, nameof(PlayRecipeVideo))))
				{
					btn.Pressed += () => PlayRecipeVideo(index);
				}
			}
			else
			{
				// LOCKED
				btn.Modulate = new Color(1, 1, 1, 1.0f); 
				if (!btn.IsConnected("pressed", new Callable(this, nameof(PlayLockedSound))))
				{
					btn.Pressed += PlayLockedSound;
				}
			}
		}
	}

	private void PlayLockedSound()
	{
		if (LockedSound != null) LockedSound.Play();
	}

	// ♡⸝⸝ BUTTON HOVER EFFECT LOGIC ♡⸝⸝
	private void SetupButtonHover(TextureButton btn)
	{
		if (btn == null) return;
		
		btn.MouseEntered += () => btn.Modulate = new Color(0.8f, 0.8f, 0.8f, 1.0f);
		btn.MouseExited += () => btn.Modulate = new Color(1.0f, 1.0f, 1.0f, 1.0f);
	}

	// ♡⸝⸝ VIDEO LOGIC ♡⸝⸝
	private void PlayRecipeVideo(int index)
	{
		GD.Print($"Playing Recipe {index}");
		PlayClick();

		if (RecipeStreams != null && index < RecipeStreams.Length && RecipeStreams[index] != null)
		{
			RecipeVideoPlayer.Stream = RecipeStreams[index];
		}
		
		if (RecipePopup != null) RecipePopup.Visible = false;
		if (MusicPlayer != null) MusicPlayer.StreamPaused = true;

		if (RecipeVideoPlayer != null)
		{
			RecipeVideoPlayer.Visible = true;
			RecipeVideoPlayer.VolumeDb = VideoVolumeDb;
			RecipeVideoPlayer.Play();
			RecipeVideoPlayer.Paused = false;
			if (PauseButton != null && PauseIcon != null) PauseButton.TextureNormal = PauseIcon;
		}
	}

	// ♡⸝⸝ NAVIGATION & UI ♡⸝⸝
	public void OnClosetPressed()
	{
		PlayClick();
		KitchenPhoto.Visible = false; 
		ClosetPhoto.Visible = true;
		if (KitchenTitle != null) KitchenTitle.Visible = false;
		if (ClosetTitle != null) ClosetTitle.Visible = true;
		
		if (Character != null)
		{
			Character.RegionEnabled = true;
			if (ClosetPosition != Vector2.Zero) Character.Position = ClosetPosition;
		}
	}

	public void OnBackToKitchenPressed()
	{
		PlayClick();
		KitchenPhoto.Visible = true; 
		ClosetPhoto.Visible = false;
		if (KitchenTitle != null) KitchenTitle.Visible = true;
		if (ClosetTitle != null) ClosetTitle.Visible = false;

		if (Character != null)
		{
			Character.RegionEnabled = true;
			if (KitchenCropRegion.HasArea()) Character.RegionRect = KitchenCropRegion;
			if (KitchenPosition != Vector2.Zero) Character.Position = KitchenPosition;
		}

		if (RecipeVideoPlayer != null)
		{
			RecipeVideoPlayer.Stop();
			RecipeVideoPlayer.Visible = false;
		}
		if (MusicPlayer != null) MusicPlayer.StreamPaused = false;
	}

	public void OnRecipePressed() { PlayClick(); RecipePopup.Visible = true; }
	public void OnCloseRecipePressed() { PlayClick(); RecipePopup.Visible = false; }
	
	public void OnCloseVideoPressed() 
	{ 
		PlayClick(); 
		if(RecipeVideoPlayer != null) 
		{ 
			RecipeVideoPlayer.Stop(); 
			RecipeVideoPlayer.Visible = false; 
		} 
		if (MusicPlayer != null) MusicPlayer.StreamPaused = false; 
	}

	// Video controls
	public void OnPauseVideoPressed() { 
		PlayClick(); 
		if (RecipeVideoPlayer != null) { 
			RecipeVideoPlayer.Paused = !RecipeVideoPlayer.Paused; 
			if (PauseButton != null) PauseButton.TextureNormal = RecipeVideoPlayer.Paused ? PlayIcon : PauseIcon; 
		} 
	}
	public void OnRewindVideoPressed() { PlayClick(); if (RecipeVideoPlayer != null) RecipeVideoPlayer.StreamPosition = Math.Max(0, RecipeVideoPlayer.StreamPosition - 5.0); }
	public void OnSkipVideoPressed() { PlayClick(); if (RecipeVideoPlayer != null) RecipeVideoPlayer.StreamPosition += 5.0; }
	public void OnRestartVideoPressed() { PlayClick(); if (RecipeVideoPlayer != null) { RecipeVideoPlayer.StreamPosition = 0; RecipeVideoPlayer.Paused = false; RecipeVideoPlayer.Play(); if (PauseButton != null) PauseButton.TextureNormal = PauseIcon; } }

	// ♡⸝⸝ WARDROBE LOGIC ♡⸝⸝
public void OnNextOutfitPressed()
	{
		PlayClick();
		if (Outfits == null || Outfits.Count == 0) return;
		_currentOutfitIndex++;
		if (_currentOutfitIndex >= Outfits.Count) _currentOutfitIndex = 0;
		ApplyOutfit(Outfits[_currentOutfitIndex]);
	}
	
	private void ApplyOutfit(Texture2D newTexture)
	{
		if (Character == null || newTexture == null) return;
		Character.Texture = newTexture;
	}

	private void PlayClick() { if (ClickSound != null) ClickSound.Play(); }
}
