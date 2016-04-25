using SS14.Shared;
using System.Collections.Generic;


namespace SS14.Server.GameObjects.Organs.Human
{

    public class Head : ExternalOrgan
    {

        public Head(HumanHealthComponent _owner)
        {
            Owner = _owner;
            Name = "Head";
            zone = BodyPart.Head;

            Children = new List<ExternalOrgan>();

            InternalOrgans = new List<InternalOrgan>();
            InternalOrgans.Add(new Brain(Owner, this));

        }

    }



    public class Torso : ExternalOrgan
    {
        public Torso(HumanHealthComponent _owner)
        {
            Owner = _owner;
            Name = "Torso";
            zone = BodyPart.Torso;

            Children = new List<ExternalOrgan>();

            InternalOrgans = new List<InternalOrgan>();
            InternalOrgans.Add(new Heart(Owner, this));
            InternalOrgans.Add(new Lungs(Owner, this));
            InternalOrgans.Add(new Liver(Owner, this));
        }
    }



    public class RightArm : ExternalOrgan
    {
        public RightArm(HumanHealthComponent _owner)
        {
            Owner = _owner;
            Name = "Right Arm";
            zone = BodyPart.Right_Arm;

            Children = new List<ExternalOrgan>();
            InternalOrgans = new List<InternalOrgan>();
        }
    }


    public class LeftArm : ExternalOrgan
    {
        public LeftArm(HumanHealthComponent _owner)
        {
            Owner = _owner;
            Name = "Left Arm";
            zone = BodyPart.Left_Arm;

            Children = new List<ExternalOrgan>();
            InternalOrgans = new List<InternalOrgan>();
        }
    }


    public class RightLeg : ExternalOrgan
    {
        public RightLeg(HumanHealthComponent _owner)
        {
            Owner = _owner;
            Name = "Right Leg";
            zone = BodyPart.Right_Leg;

            Children = new List<ExternalOrgan>();
            InternalOrgans = new List<InternalOrgan>();
        }
    }


    public class LeftLeg : ExternalOrgan
    {
        public LeftLeg(HumanHealthComponent _owner)
        {
            Owner = _owner;
            Name = "Left Leg";
            zone = BodyPart.Left_Leg;

            Children = new List<ExternalOrgan>();
            InternalOrgans = new List<InternalOrgan>();
        }
    }







}