#include "QHBasketball.h"

#include "Components/SphereComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Net/UnrealNetwork.h"

AQHBasketball::AQHBasketball()
{
    PrimaryActorTick.bCanEverTick = false;
    bReplicates = true;
    SetReplicateMovement(true);

    Collision = CreateDefaultSubobject<USphereComponent>(TEXT("BallCollision"));
    SetRootComponent(Collision);
    Collision->InitSphereRadius(12.0f);
    Collision->SetCollisionProfileName(TEXT("PhysicsActor"));
    Collision->SetSimulatePhysics(true);
    Collision->SetEnableGravity(true);
    Collision->SetIsReplicated(true);

    BallMesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("BallMesh"));
    BallMesh->SetupAttachment(Collision);
    BallMesh->SetCollisionEnabled(ECollisionEnabled::NoCollision);
}

void AQHBasketball::SetHeldState(const bool bNewHeld)
{
    if (!HasAuthority())
    {
        return;
    }

    bIsHeld = bNewHeld;
    OnRep_IsHeld();
    ForceNetUpdate();
}

void AQHBasketball::OnRep_IsHeld()
{
    if (!Collision)
    {
        return;
    }

    Collision->SetSimulatePhysics(!bIsHeld);
    Collision->SetEnableGravity(!bIsHeld);
    Collision->SetCollisionEnabled(bIsHeld ? ECollisionEnabled::NoCollision : ECollisionEnabled::QueryAndPhysics);
}

void AQHBasketball::ResetBall(const FTransform& SpawnTransform)
{
    if (!HasAuthority())
    {
        return;
    }

    DetachFromActor(FDetachmentTransformRules::KeepWorldTransform);
    bIsHeld = false;
    SetActorTransform(SpawnTransform, false, nullptr, ETeleportType::TeleportPhysics);
    OnRep_IsHeld();
    Collision->SetPhysicsLinearVelocity(FVector::ZeroVector);
    Collision->SetPhysicsAngularVelocityInDegrees(FVector::ZeroVector);
    ForceNetUpdate();
}

void AQHBasketball::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    DOREPLIFETIME(AQHBasketball, bIsHeld);
}
