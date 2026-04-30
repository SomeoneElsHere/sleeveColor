using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Color = Microsoft.Xna.Framework.Color;

namespace ColorSleeves
{
    
    internal sealed class ModEntry : Mod
    {
        Dictionary<string, Color> colorDict;
        //SLIDERS
        SliderBar slideR;
        SliderBar slideG;
        SliderBar slideB;
        //PREV MOUSE POSITIONS
        int PrevMouseXposR = 0;
        int PrevMouseXposG = 0;
        int PrevMouseXposB = 0;
        //debug variables that I need to keep.
        int SliderX = 80;
        int SliderY = 550;
        int sliderDist = 30;
        
        
        public override void Entry(IModHelper helper)
        {
            helper.Events.Content.AssetReady += this.OnAssetReady;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Player.InventoryChanged += this.OnInventoryChanged;
            colorDict = new Dictionary<string, Color>();
            readDat();
            
            

        }

        public Color colorConv(Color color, byte darkness) //makes a color darker by an amount
        {
            Color a = new Color((int)color.R,color.G,color.B,255);
            if(a.R > darkness)
            {
                a.R -= darkness;
            }
            if (a.G > darkness)
            {
                a.G -= darkness;
            }
            if (a.B > darkness)
            {
                a.B -= darkness;
            }
            return a;
        }

        public void readDat()
        {
            if (File.Exists("colorData.txt")) //if file exists
            {
                //read it.
                string[] lines;
                lines = File.ReadAllLines("colorData.txt");
                for (int i = 0; i < lines.Length; i++)
                {
                    var parts = lines[i].Split(" :".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    string id = parts[0];
                    int R = int.Parse(parts[1]);
                    int G = int.Parse(parts[2]);
                    int B = int.Parse(parts[3]);
                    Color c = new Color(R, G, B, 255);
                    
                    colorDict.Add(id, c);
                    
                }
            }
            else
            {
                File.Create("colorData.txt");
            }

        }

        public void setState(int R, int G, int B)
        {
            if (Game1.player.ShirtHasSleeves() && Game1.player.shirtItem.Value != null) //if there are sleeves
            {
                int mod = 2;
                Color c = new Color(R*mod, G*mod, B*mod, 255); //mod values by 2 so they are scaled to rgb
                if (colorDict.ContainsKey(Game1.player.shirtItem.Value.ItemId)) //clean dict of dupes
                {
                    colorDict.Remove(Game1.player.shirtItem.Value.ItemId);
                }
                colorDict.Add(Game1.player.shirtItem.Value.ItemId, c); //add
                //file stream chicanery
                //read from file, make objects, put in dict.
                FileStream fs = File.Create("colorData.txt");
                for (int i = 0; i < colorDict.Count; i++)
                {
                    String id = colorDict.Keys.ElementAt(i);
                    Color cd = colorDict[id];
                    string line = id + " : " + cd.R + " : " + cd.G + " : " + cd.B + "\n";
                    var byts = Encoding.UTF8.GetBytes(line);
                    fs.Write(byts);
                }
                fs.Close();
                changeColor(); //then change the color again to refresh.
            }
            
        }
        public void changeColor()
        {
            
            if (Game1.player != null && Game1.player.shirtItem.Value != null && colorDict.ContainsKey(Game1.player.shirtItem.Value.ItemId))
            {
                //Console.WriteLine(colorDict.ContainsKey(Game1.player.shirtItem.Value.ItemId));
                //assembly stuff for pixelData
                var va = Assembly.GetAssembly(typeof(StardewValley.FarmerRenderer))
                                 .GetType("StardewValley.FarmerRenderer")
                                 .GetMember("baseTexture", BindingFlags.Instance |
                                BindingFlags.NonPublic |
                                BindingFlags.Public);
                Texture2D baseTexture = (Texture2D)((System.Reflection.FieldInfo)va.GetValue(0)).GetValue(Game1.player.FarmerRenderer);
                if (baseTexture == null)
                {
                    return;
                }
                Microsoft.Xna.Framework.Color[] pixelData = new Microsoft.Xna.Framework.Color[baseTexture.Width * baseTexture.Height];

                //SET BASE TEXTURE DATA
                baseTexture.GetData(pixelData);

                //Get Shirt texture and shirt index
                Texture2D shirtTexture = null;
                int shirtIndex = 0;
                Game1.player.GetDisplayShirt(out shirtTexture, out shirtIndex);
                Microsoft.Xna.Framework.Color[] shirtData = new Microsoft.Xna.Framework.Color[shirtTexture.Bounds.Width * shirtTexture.Bounds.Height];


                //get and set shirt data
                shirtTexture.GetData(shirtData);
                int dyeIndex = (shirtIndex * 8 / 128 * 32 * shirtTexture.Bounds.Width + shirtIndex * 8 % 128 + shirtTexture.Width * 4) + 128;


                ///IMPORTANT STUFF IF YOU WANT TO CHANGE COLOR//
                Color sleeve;
                Color secondary;
                Color primary;
                colorDict.TryGetValue(Game1.player.shirtItem.Value.ItemId, out primary);
                secondary = colorConv(primary, 20);
                sleeve = colorConv(primary, 50);

                
                shirtData[dyeIndex] = sleeve; //SET COLOR HERE (color for edges)
                shirtData[dyeIndex].A = byte.MaxValue;
                shirtData[dyeIndex - shirtTexture.Width] = secondary; //secondary color?
                shirtData[dyeIndex - shirtTexture.Width * 2] = primary; //primary color?
                ///IGNORE THE REST OF THIS//

                shirtTexture.SetData(shirtData);


                //apply that sleeve color!
                Game1.player.FarmerRenderer.ApplySleeveColor(Game1.player.FarmerRenderer.textureName.Value, pixelData, Game1.player);
                baseTexture.SetData(pixelData);
            }
        }
        public void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is GameMenu && ((GameMenu)Game1.activeClickableMenu).GetCurrentPage() is InventoryPage)
            {
                Game1.DrawBox(SliderX, 550, 120, 100); //do the background box
                
                
                

                if (slideR != null && slideG != null && slideB != null) //if init
                {
                    //Console.WriteLine(Game1.getMouseX() + ":" + Game1.getMouseY());
                    //SLIDER RED
                    if (Game1.getMouseX() <SliderX+100 && Game1.getMouseX() > SliderX && Game1.getMouseY() < SliderY+sliderDist && Game1.getMouseY() > SliderY)
                    { //if in bounds...
                        
                        if (Game1.input.GetMouseState().LeftButton == ButtonState.Pressed)
                        { //and left button pressed
                            
                            int current = Game1.getMouseX(); //get mouse x
                            if(current != PrevMouseXposR) //if mouse is in same place, do nothing
                            {
                               
                                int dif = current - PrevMouseXposR;
                                slideR.changeValueBy(dif); //change value by the change in x
                                setState(slideR.value,slideG.value,slideB.value); 
                            }
                            PrevMouseXposR = current;
                        }
                    }
                    slideR.draw(Game1.spriteBatch); //show it to the screen
                    
                
                    //slider GREEN
                    if (Game1.getMouseX() < SliderX+100 && Game1.getMouseX() > SliderX && Game1.getMouseY() < SliderY+sliderDist*2 && Game1.getMouseY() > SliderY+sliderDist)
                    {
                        if (Game1.input.GetMouseState().LeftButton == ButtonState.Pressed)
                        {
                            int current = Game1.getMouseX();
                            if (current != PrevMouseXposG)
                            {
                                int dif = current - PrevMouseXposG;
                                slideG.changeValueBy(dif);
                                setState(slideR.value, slideG.value, slideB.value);
                            }
                            PrevMouseXposG = current;
                        }
                    }
                    slideG.draw(Game1.spriteBatch);

                    //slider BLUE
                    if (Game1.getMouseX() < SliderX+100 && Game1.getMouseX() > SliderX && Game1.getMouseY() < SliderY+sliderDist*3 && Game1.getMouseY() > SliderY+sliderDist*2)
                    {
                        if (Game1.input.GetMouseState().LeftButton == ButtonState.Pressed)
                        {
                            int current = Game1.getMouseX();
                            if (current != PrevMouseXposB)
                            {
                                int dif = current - PrevMouseXposB;
                                slideB.changeValueBy(dif);
                                setState(slideR.value, slideG.value, slideB.value);
                            }
                            PrevMouseXposB = current;
                        }
                    }
                    slideB.draw(Game1.spriteBatch);

                }

                Texture2D mousecursor = Game1.mouseCursors;
                //draw the mouse cursor on top since we drew over it.
                Game1.spriteBatch.Draw(mousecursor, new Vector2(Game1.getMouseX(), Game1.getMouseY()), Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, Game1.mouseCursor, 16, 16), Color.White * Game1.mouseCursorTransparency, 0f, Vector2.Zero, 4f + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);

            }
        }
        public void OnMenuChanged(object? sender, MenuChangedEventArgs e) //create slider variables once per refresh of page
        {
           
            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is GameMenu && ((GameMenu)Game1.activeClickableMenu).GetCurrentPage() is InventoryPage)
            {
                PrevMouseXposR = SliderX;
                PrevMouseXposG = SliderX;
                PrevMouseXposB = SliderX;
                slideR = new SliderBar(SliderX, SliderY+5, 0);
                slideG = new SliderBar(SliderX, SliderY+sliderDist+5, 0);
                slideB = new SliderBar(SliderX, SliderY+sliderDist*2+5, 0);
            }
        }



        public void OnAssetReady(object? sender, AssetReadyEventArgs e)
        {
            if(e.NameWithoutLocale.IsEquivalentTo("Characters/Farmer/farmer_base") || e.NameWithoutLocale.IsEquivalentTo("Characters/Farmer/farmer_girl_base") || e.NameWithoutLocale.IsEquivalentTo("Characters/Farmer/farmer_base_bald") || e.NameWithoutLocale.IsEquivalentTo("Characters/Farmer/farmer_girl_base_bald"))
            {
                changeColor();
                
            }
        }

        public void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            changeColor();
        }

        public void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            changeColor();
        }

    }
}
