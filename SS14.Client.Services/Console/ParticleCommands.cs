// TODO: Re-add these.

/*
switch (command)
{
				case "addparticles": //This is only clientside.
		if (args.Count >= 3)
		{
			Entity target = null;
			if (args[1].ToLowerInvariant() == "player")
			{
				var plrMgr = IoCManager.Resolve<IPlayerManager>();
				if (plrMgr != null)
					if (plrMgr.ControlledEntity != null) target = plrMgr.ControlledEntity;
			}
			else
			{
				var entMgr = IoCManager.Resolve<IEntityManagerContainer>();
				if (entMgr != null)
				{
					int entUid = int.Parse(args[1]);
					target = entMgr.EntityManager.GetEntity(entUid);
				}
			}

			if (target != null)
			{
				if (!target.HasComponent(ComponentFamily.Particles))
				{
					var entMgr = IoCManager.Resolve<IEntityManagerContainer>();
					var compo = (IParticleSystemComponent)entMgr.EntityManager.ComponentFactory.GetComponent("ParticleSystemComponent");
					target.AddComponent(ComponentFamily.Particles, compo);
				}
				else
				{
					var entMgr = IoCManager.Resolve<IEntityManagerContainer>();
					var compo = (IParticleSystemComponent)entMgr.EntityManager.ComponentFactory.GetComponent("ParticleSystemComponent");
					target.AddComponent(ComponentFamily.Particles, compo);
				}
			}
		}
		SendServerConsoleCommand(text); //Forward to server.
		break;

	case "removeparticles":
		if (args.Count >= 3)
		{
			Entity target = null;
			if (args[1].ToLowerInvariant() == "player")
			{
				var plrMgr = IoCManager.Resolve<IPlayerManager>();
				if (plrMgr != null)
					if (plrMgr.ControlledEntity != null) target = plrMgr.ControlledEntity;
			}
			else
			{
				var entMgr = IoCManager.Resolve<IEntityManagerContainer>();
				if (entMgr != null)
				{
					int entUid = int.Parse(args[1]);
					target = entMgr.EntityManager.GetEntity(entUid);
				}
			}

			if (target != null)
			{
				if (target.HasComponent(ComponentFamily.Particles))
				{
					IParticleSystemComponent compo = (IParticleSystemComponent)target.GetComponent(ComponentFamily.Particles);
					compo.RemoveParticleSystem(args[2]);
				}
			}
		}
		SendServerConsoleCommand(text); //Forward to server.
		break;
}*/