// Run the actual geometry/board/render bookkeeping code outside Unity.
// Small managed fixtures stand in for native UI objects; this is not a Play Mode test.
using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UnityEngine;

namespace UnityEngine
{
    public sealed class RequireComponent : Attribute { public RequireComponent(Type type) {} }
    public sealed class SerializeField : Attribute {}
    public sealed class Min : Attribute { public Min(float value) {} }
    public sealed class Tooltip : Attribute { public Tooltip(string value) {} }
    public class MonoBehaviour { public Transform transform = new Transform(); }
    public class Transform { public Vector3 position; public Vector3 lossyScale = new Vector3(1,1,1); public Vector3 InverseTransformPoint(Vector3 p) => p; }
    public class RectTransform : Transform {}
    public class Sprite { public string name; public Sprite(string name) { this.name = name; } }
    public struct Vector2Int
    {
        public int x,y; public Vector2Int(int x,int y) { this.x=x; this.y=y; }
        public static Vector2Int zero => new Vector2Int(0,0);
        public static Vector2Int operator +(Vector2Int a,Vector2Int b) => new Vector2Int(a.x+b.x,a.y+b.y);
        public static Vector2Int operator -(Vector2Int a,Vector2Int b) => new Vector2Int(a.x-b.x,a.y-b.y);
        public static Vector2Int Min(Vector2Int a,Vector2Int b) => new Vector2Int(Math.Min(a.x,b.x),Math.Min(a.y,b.y));
        public static Vector2Int Max(Vector2Int a,Vector2Int b) => new Vector2Int(Math.Max(a.x,b.x),Math.Max(a.y,b.y));
    }
    public struct Vector2
    {
        public float x,y; public Vector2(float x,float y) { this.x=x; this.y=y; }
        public static explicit operator Vector2(Vector3 p) => new Vector2(p.x,p.y);
        public static Vector2 operator -(Vector2 a,Vector2 b) => new Vector2(a.x-b.x,a.y-b.y);
        public static Vector2 operator /(Vector2 a,float b) => new Vector2(a.x/b,a.y/b);
    }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; } }
    public static class Mathf { public static int Max(int a,int b)=>Math.Max(a,b); public static int RoundToInt(float value)=>(int)Math.Round(value); }
}
public sealed class BlockArt { public Sprite empty; public Sprite[] blocks; }
public sealed class BlockPiece
{
    public Vector2Int[] Shape; public Sprite[] CatParts; public Sprite SmallCat; public float CatAngle;
    public Vector3 FirstBlockPosition;
}
public sealed class BlockCell
{
    public Vector2Int Coordinate; public bool IsConfigured => true; public Transform transform = new Transform();
    public Sprite shown; public float angle,opacity,size;
    public void Show(Sprite sprite,float opacity=1,float angle=0,float size=0) { shown=sprite;this.opacity=opacity;this.angle=angle;this.size=size; }
}
public static class CatRenderChecks
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); Console.WriteLine("PASS "+message); }
    public static void Main(string[] args)
    {
        var configured=new List<BlockCatPose>();
        using(var json=JsonDocument.Parse(File.ReadAllText(args[0])))
        {
            foreach(var entry in json.RootElement.EnumerateArray())
            {
                var points=new List<Vector2Int>(); foreach(var point in entry.GetProperty("shape").EnumerateArray())
                    points.Add(new Vector2Int(point.GetProperty("x").GetInt32(),point.GetProperty("y").GetInt32()));
                var parts=new Sprite[entry.GetProperty("parts").GetArrayLength()];
                for(int i=0;i<parts.Length;i++) parts[i]=new Sprite("catalog part "+i);
                configured.Add(new BlockCatPose {shape=points.ToArray(),parts=parts,small=new Sprite(entry.GetProperty("breed").GetString())});
            }
        }
        foreach(var target in BlockBoard.Shapes)
            if(!configured.Exists(pose=>pose.Match(target,out _,out _))) throw new Exception("Configured cat catalog has a missing shape");
        Check(configured.Count==17,"configured 17 cat poses cover every one of the 27 real game shapes");
        int covered=0;
        foreach(var target in BlockBoard.Shapes)
        {
            var parts=new Sprite[target.Length]; for(int i=0;i<parts.Length;i++) parts[i]=new Sprite("part"+i);
            var pose=new BlockCatPose {shape=target,parts=parts,small=new Sprite("kitten")};
            var rotated=new Vector2Int[target.Length]; var min=new Vector2Int(int.MaxValue,int.MaxValue);
            for(int i=0;i<target.Length;i++) { rotated[i]=new Vector2Int(-target[i].y,target[i].x); min=Vector2Int.Min(min,rotated[i]); }
            for(int i=0;i<target.Length;i++) rotated[i]-=min;
            if(!pose.Match(rotated,out var matched,out float angle)) throw new Exception("Rotated pose did not match");
            for(int i=0;i<target.Length;i++)
            {
                // Each returned sprite must map its original coordinate through the chosen rotation.
                int source=Array.IndexOf(parts,matched[i]); var p=target[source];
                var offset=new Vector2Int(int.MaxValue,int.MaxValue);
                for(int j=0;j<target.Length;j++) { var q=target[j]; for(int t=0;t<(int)angle/90;t++) q=new Vector2Int(-q.y,q.x); offset=Vector2Int.Min(offset,q); }
                for(int t=0;t<(int)angle/90;t++) p=new Vector2Int(-p.y,p.x); p-=offset;
                if(p.x!=rotated[i].x || p.y!=rotated[i].y) throw new Exception("Rotated part identity mismatch");
            }
            covered++;
        }
        Check(covered==27,"actual CatPose rotation maps all 27 puzzle shapes without losing sprite identity");
        var cells=new BlockCell[64];
        for(int y=0;y<8;y++) for(int x=0;x<8;x++) cells[y*8+x]=new BlockCell {Coordinate=new Vector2Int(x,y)};
        var grid=new BlockGridView(); typeof(BlockGridView).GetField("cells",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(grid,cells);
        Check(grid.ValidateCells(),"grid fixture has 64 unique cells");
        var empty=new Sprite("empty"); var art=new BlockArt {empty=empty,blocks=new[]{new Sprite("classic")}};
        var shape=new[]{new Vector2Int(0,0),new Vector2Int(0,1),new Vector2Int(0,2)};
        var head=new Sprite("head"); var body=new Sprite("body"); var tail=new Sprite("tail"); var kitten=new Sprite("ginger kitten");
        var piece=new BlockPiece {Shape=shape,CatParts=new[]{head,body,tail},SmallCat=kitten,CatAngle=90};
        var board=new BlockBoard(); board.Place(shape,Vector2Int.zero,1,out _); grid.RememberCat(piece,Vector2Int.zero); grid.Render(board,art);
        Check(cells[0].shown==head && cells[8].shown==body && cells[16].shown==tail && cells[0].angle==90,"placed whole cat retains body parts and orientation");
        for(int x=1;x<7;x++) board.Cells[x,1]=1;
        board.Place(new[]{Vector2Int.zero},new Vector2Int(7,1),1,out int lines); grid.Render(board,art);
        Check(lines==1 && cells[8].shown==empty && cells[0].shown==kitten && cells[16].shown==kitten && cells[0].angle==0,"partial row clear replaces surviving body segments with upright same-breed kittens");
        var black=new Sprite("black kitten"); var next=new BlockPiece {Shape=new[]{Vector2Int.zero},CatParts=new[]{black},SmallCat=black};
        board.Place(next.Shape,new Vector2Int(0,1),1,out _); grid.RememberCat(next,new Vector2Int(0,1)); grid.Render(board,art);
        Check(cells[8].shown==black && cells[0].shown==kitten,"refilling a cleared cell never resurrects an old cat segment");
        grid.ResetCats(); grid.Render(new BlockBoard(),art);
        Check(Array.TrueForAll(cells,c=>c.shown==empty),"restart discards all previous cat placements");
        grid.Preview(shape,Vector2Int.zero,art.blocks[0],piece.CatParts,90);
        Check(cells[8].shown==body && cells[8].opacity==.55f && cells[8].angle==90,"placement ghost uses the same cat sprites and rotation");
    }
}
